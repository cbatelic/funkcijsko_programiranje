namespace AvaloniaApplication1

open System
open Avalonia
open AvaloniaApplication1.Models.Node
open AvaloniaApplication1.Logic.Propagation
open FSharp.Data.Adaptive

module Program =

    [<CompiledName "BuildAvaloniaApp">]
    let buildAvaloniaApp () =
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace(areas = Array.empty)

    let printNodeValues (nodes: Node list) =
        nodes |> List.iter (fun n ->
            let calculatedValue = calculateNodeValue nodes n.Id |> AVal.force
            match calculatedValue with
            | Some value -> printfn $"Vrijednost čvora {n.Name}: {value}"
            | None -> printfn $"Čvor {n.Name} nije moguće izračunati."
        )

    let testNodes =
        [
            { Id = 1; Name = "A"; NodeType = Input; Inputs = []; Value = Some 3.0 }
            { Id = 2; Name = "B"; NodeType = Input; Inputs = []; Value = Some 2.0 }
            { Id = 3; Name = "Sum1"; NodeType = Sum; Inputs = [1; 2]; Value = None }
            { Id = 4; Name = "Multiply1"; NodeType = Multiply; Inputs = [1; 3]; Value = None }
            { Id = 5; Name = "Output1"; NodeType = Output; Inputs = [4]; Value = None }
        ]

    [<EntryPoint; STAThread>]
    let main argv =
        printNodeValues testNodes
        buildAvaloniaApp().StartWithClassicDesktopLifetime(argv)
