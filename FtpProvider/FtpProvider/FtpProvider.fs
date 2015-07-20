module FtpTypeProviderImplementation

open System
open System.Net
open System.IO
open System.Reflection
open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices

/// Get the directories and files in an FTP site using anonymous login
let getFtpDirectory (site:string, user:string, pwd:string) = 
    let request = 
        match WebRequest.Create(site) with 
        | :? FtpWebRequest as f -> f
        | _ -> failwith (sprintf "site '%s' did not result in an FTP request. Do you need to add prefix 'ftp://' ?" site)
    request.Method <- WebRequestMethods.Ftp.ListDirectoryDetails
    request.Credentials <- NetworkCredential(user, pwd)

    use response = request.GetResponse() :?> FtpWebResponse
    
    use responseStream = response.GetResponseStream()
    use reader = new StreamReader(responseStream)
    let contents = 
        [ while not reader.EndOfStream do 
             yield reader.ReadLine().Split([| ' ';'\t' |],StringSplitOptions.RemoveEmptyEntries) ]

    let dirs = 
        [ for c in contents do 
            if c.Length > 1 then 
               if c.[0].StartsWith("d") then yield Seq.last c ]

    let files = 
        [ for c in contents do 
            if c.Length > 1 then 
               if c.[0].StartsWith("-") then yield Seq.last c ]

    files, dirs

// getFtpDirectory  "ftp://ftp.ncbi.nlm.nih.gov/"


[<TypeProvider>]
type FtpProviderImpl(config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces ()
    let nameSpace = "FSharp.Management"
    let asm = Assembly.GetExecutingAssembly()

    // Recursive, on-demand adding of types
    let createTypes (typeName, site, user, pwd) = 
        let rec addTypes (site:string, td:ProvidedTypeDefinition) =
        
            td.AddMembersDelayed(fun () -> 
                let files, dirs = getFtpDirectory (site, user, pwd)

                [ for dir in dirs do 
                    let nestedType = ProvidedTypeDefinition(dir, Some typeof<obj>)
                    addTypes(site + dir + "/", nestedType)
                    yield nestedType :> MemberInfo 

                  for file in files do 
                    //let nestedType = ProvidedTypeDefinition(file, Some typeof<obj>)
                    let myProp = ProvidedLiteralField("Contents", typeof<string>, site + file)
    (*
                                    GetterCode = (fun args -> 
                                        <@@ let request = WebRequest.Create(site + file) :?> FtpWebRequest
                                            request.Method <- WebRequestMethods.Ftp.DownloadFile
                                            request.Credentials <- new NetworkCredential ("anonymous","janeDoe@contoso.com");
                                            let response = request.GetResponse() :?> FtpWebResponse
                                            use responseStream = response.GetResponseStream()
                                            use reader = new StreamReader(responseStream)
                                            reader.ReadToEnd() @@>))
    *)
                    //nestedType.AddMember myProp 
                    yield myProp :> MemberInfo ] )
        let actualType = ProvidedTypeDefinition(asm, nameSpace, typeName, Some typeof<obj>)
        addTypes(site, actualType)
        actualType

    let _ = 
        let topType = ProvidedTypeDefinition(asm, nameSpace, "FtpProvider", Some typeof<obj>)
        let siteParam = 
           let p = ProvidedStaticParameter("Url",typeof<string>) 
           p.AddXmlDoc("The URL of the FTP site, including ftp://")
           p
        let userParam = 
           let p = ProvidedStaticParameter("User",typeof<string>, "anonymous") 
           p.AddXmlDoc("The user of the FTP site (default 'anonymous')")
           p
        let pwdParam = 
           let p = ProvidedStaticParameter("Password",typeof<string>, "janedoe@contoso.com") 
           p.AddXmlDoc("The password used to access the FTP site (default 'janedoe@contoso.com')")
           p
        let staticParams = [ siteParam; userParam; pwdParam ]
        topType.DefineStaticParameters(staticParams, (fun typeName args -> 
            let site = args.[0] :?> string
            let user = args.[1] :?> string
            let pwd = args.[2] :?> string
            createTypes(typeName, site, user, pwd)))
        this.AddNamespace(nameSpace, [topType])

[<assembly:TypeProviderAssembly>]
do ()