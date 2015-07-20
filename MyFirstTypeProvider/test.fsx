#r @"MyFirstTypeProvider\bin\Debug\MyFirstTypeProvider.dll"



type BioData = FSharp.Data.FtpProvider< "ftp://ftp.ncbi.nlm.nih.gov/", User="anonymous", Pwd="mypwd" > 


let MyFunction() = BioData.genomes.Drosophila_melanogaster.``RELEASE_4.1``.CHR_2 .``NT_033778.faa``.Contents



