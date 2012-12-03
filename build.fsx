#I @"tools\fake"
#r "FakeLib.dll"

open Fake

let buildDir = @".\build\"
let testDir =  @".\test\"

let nunitPath = @".\tools\NUnit"

let appReferences  = 
    !+ @"src\Moiety\**\*.fsproj" |> Scan
    
let testReferences = 
    !+ @"src\tests\**\*.fsproj" |> Scan

Target "Clean" (fun _ ->
    CleanDirs [buildDir; testDir]
)

Target "BuildApp" (fun _ ->
    MSBuildRelease buildDir "Build" appReferences
        |> Log "AppBuild-Output:"
)

Target "BuildTest" (fun _ ->
    MSBuildRelease testDir "Build" testReferences
        |> Log "TestBuild-Output:"
)

Target "Test" (fun _ ->
 !! (testDir + @"\*.dll") 
        |> NUnit (fun p -> 
            {p with 
                ToolPath = nunitPath; 
                DisableShadowCopy = true; 
                OutputFile = testDir + @"TestResults.xml"})
)

"Clean"
    ==> "BuildApp"
    ==> "BuildTest"
    ==> "Test"

Run "Test"