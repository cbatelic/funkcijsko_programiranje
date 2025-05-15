
namespace AvaloniaApplication1.ViewModels

open System.Collections.ObjectModel
open System.Windows.Input
open CommunityToolkit.Mvvm.ComponentModel
open CommunityToolkit.Mvvm.Input
open AvaloniaApplication1.Models.RuntimeNode
open FSharp.Data.Adaptive

type MainWindowViewModel() =
    inherit ObservableObject()

    let nodes = ObservableCollection<RuntimeNode>()
    let mutable selectedOperation = "Zbrajanje"
    let operations = [ "Zbrajanje"; "Oduzimanje" ]
    let mutable newValue = ""
    let selectedNodes = ResizeArray<RuntimeNode>()

    let mutable calculationResult = 0.0

    member this.Nodes = nodes

    member this.NewValue
        with get() = newValue
        and set v = this.SetProperty(&newValue, v) |> ignore

    member this.SelectedOperation
        with get() = selectedOperation
        and set v = this.SetProperty(&selectedOperation, v) |> ignore

    member this.Operations = operations

    member this.CalculationResult
        with get() = calculationResult
        and set v = this.SetProperty(&calculationResult, v) |> ignore

    member this.SelectedNodes = selectedNodes

    member this.ToggleSelectionCommand : ICommand =
        RelayCommand<RuntimeNode>(fun node ->
            if selectedNodes.Contains(node) then
                selectedNodes.Remove(node) |> ignore
            else
                selectedNodes.Add(node)
        )

    member this.CalculateCommand : ICommand =
        RelayCommand(fun () ->
            let values =
                selectedNodes
                |> Seq.choose (fun n -> n.Value)
                |> Seq.toList

            match selectedOperation with
            | "Zbrajanje" -> this.CalculationResult <- values |> List.sum
            | "Oduzimanje" ->
                match values with
                | h :: t -> this.CalculationResult <- List.fold (-) h t
                | _ -> this.CalculationResult <- 0.0
            | _ -> ()
        )

    member this.AddInputCommand : ICommand =
        RelayCommand(fun () ->
            match System.Double.TryParse(newValue) with
            | true, v ->
                let node = RuntimeNode(System.Guid.NewGuid().GetHashCode(), $"Input {nodes.Count + 1}", AVal.constant (Some v))
                nodes.Add(node)
                this.NewValue <- ""
            | _ -> ()
        )

    member this.AddOperationCommand : ICommand =
        RelayCommand(fun () ->
            let node = RuntimeNode(System.Guid.NewGuid().GetHashCode(), selectedOperation, AVal.constant None)
            nodes.Add(node)
        )
