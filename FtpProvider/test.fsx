#r @"FtpProvider\bin\Debug\FtpTypeProvider.dll"

type F = FSharp.Management.FtpProvider<"ftp://ftp.ncbi.nlm.nih.gov/"> 

F.genomes.Drosophila_melanogaster.``RELEASE_4.1``.CHR_2.``NT_033778.faa``.Contests

// F.genomes.Buceros_rhinoceros_silvestris.RNA.``Gnomon.mRNA.fsa.gz``
