module Mavnn.Blog.TypeProvider

open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices
open System
open System.Reflection
open System.IO
open System.Net
let site = "ftp://ftp.ncbi.nlm.nih.gov/"


/// Get the directories and files in an FTP site using anonymous login
let getFtpDirectory (site:string, userName:string , pwd:string) = 
    let request = WebRequest.Create(site) :?> FtpWebRequest
    request.Method <- WebRequestMethods.Ftp.ListDirectoryDetails

    // This example assumes the FTP site uses anonymous logon.
    request.Credentials <- new NetworkCredential (userName, pwd);

    let response = request.GetResponse() :?> FtpWebResponse
    
    let responseStream = response.GetResponseStream()
    let reader = new StreamReader(responseStream)
    let contents = 
        [ while not reader.EndOfStream do 
             yield reader.ReadLine().Split([| ' ';'\t' |],StringSplitOptions.RemoveEmptyEntries) ]

    let dirs = 
        [ for c in contents do 
                if c.[0].StartsWith("d") then yield Seq.last c ]

    let files = 
        [ for c in contents do 
                if c.[0].StartsWith("-") then yield Seq.last c ]

    files, dirs




[<TypeProvider>]
type MavnnProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces ()

    let ns = "FSharp.Data"
    let asm = Assembly.GetExecutingAssembly()

    let rec addFtpTypesIntoTypeDefinition (userName, pwd, ftpUrl, td: ProvidedTypeDefinition ) =
        
        td.AddMembersDelayed(fun () -> 
            
            let files, dirs = getFtpDirectory (ftpUrl, userName, pwd)

            [ for dir in dirs do 
                let nestedType = ProvidedTypeDefinition(dir, Some typeof<obj>)
                addFtpTypesIntoTypeDefinition(userName, pwd, ftpUrl + dir + "/", nestedType)
                yield nestedType  :> MemberInfo 

              for file in files do 
                let nestedType = ProvidedTypeDefinition(file, Some typeof<obj>)
                let myProp = ProvidedProperty("Contents", typeof<string>, IsStatic = true,
                                GetterCode = (fun args -> 
                                    <@@ let request = WebRequest.Create(ftpUrl + file) :?> FtpWebRequest
                                        request.Method <- WebRequestMethods.Ftp.DownloadFile
                                        request.Credentials <- new NetworkCredential (userName, pwd);
                                        let response = request.GetResponse() :?> FtpWebResponse
                                        use responseStream = response.GetResponseStream()
                                        use reader = new StreamReader(responseStream)
                                        reader.ReadToEnd() @@>))
                nestedType.AddMember myProp 
                yield nestedType :> MemberInfo ] )

    do
        let topType = ProvidedTypeDefinition(asm, ns, "FtpProvider", Some typeof<obj>)
        let staticParams = 
           [ ProvidedStaticParameter("Url",typeof<string>) 
             ProvidedStaticParameter("User",typeof<string>, "anonymous") 
             ProvidedStaticParameter("Pwd",typeof<string>, "janeDoe@anonymous.org") ]

        topType.DefineStaticParameters(staticParams, (fun typeName args -> 
            let site = args.[0] :?> string
            let userName = args.[1] :?> string
            let pwd = args.[2] :?> string
            let actualType = ProvidedTypeDefinition(asm, ns, typeName, Some typeof<obj>)
            addFtpTypesIntoTypeDefinition(userName, pwd, site, actualType)
            actualType))
        this.AddNamespace(ns, [ topType ] )




[<assembly:TypeProviderAssembly>]
do ()
