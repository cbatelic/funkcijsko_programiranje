﻿namespace AvaloniaApplication1.ViewModels

open System.Collections.ObjectModel
open System.Windows.Input
open CommunityToolkit.Mvvm.ComponentModel
open CommunityToolkit.Mvvm.Input
open AvaloniaApplication1.Models.RuntimeNode
open AvaloniaApplication1.Models.Node
open AvaloniaApplication1.Logic.Propagation
open FSharp.Data.Adaptive
open System.Diagnostics
open Microsoft.FSharp.Collections

type MainWindowViewModel() =
    inherit ObservableObject()

    let nodes = ObservableCollection<RuntimeNode>()
    let mutable selectedOperation = "Odaberi"
    let operations = [ "Odaberi"; "Zbrajanje"; "Oduzimanje"; "Množenje"; "Dijeljenje"; "Korijen" ]
    let mutable newValue = ""
    let selectedNodes = ResizeArray<RuntimeNode>()
    let mutable calculationResult = 0.0

    let calculationPerformed = Event<(int * string) list * string * float>()
    let clearAllRequested = Event<unit>() // ✅ novi event

    member this.CalculationPerformed = calculationPerformed.Publish
    member this.ClearAllRequested = clearAllRequested.Publish

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
            node.IsSelected <- true 
            if selectedNodes.Contains(node) then
                selectedNodes.Remove(node) |> ignore
            else
                selectedNodes.Add(node)

            match node.Name with
            | "Zbrajanje" | "Oduzimanje" | "Množenje" | "Dijeljenje" | "Korijen" ->
                this.SelectedOperation <- node.Name
            | _ -> ()
        )

    member this.CalculateCommand : ICommand =
        RelayCommand(fun () ->
            if selectedNodes.Count > 0 && not (System.String.IsNullOrWhiteSpace selectedOperation) then
                let inputNodes =
                    selectedNodes
                    |> Seq.choose (fun n ->
                        match n.Value with
                        | Some v ->
                            Some {
                                Id = n.Id
                                Name = n.Name
                                NodeType = NodeType.Input
                                Inputs = []
                                Value = Some v
                            }
                        | None -> None)
                    |> Seq.toList

                Debug.WriteLine($"📣 Triggering event: {inputNodes.Length} inputs, operation: {selectedOperation}")

                let inputIds = inputNodes |> List.map (fun n -> n.Id)
                let newNodeId = System.Guid.NewGuid().GetHashCode()

                let virtualNode = {
                    Id = newNodeId
                    Name = "VirtualCalc"
                    NodeType =
                        match selectedOperation with
                        | "Zbrajanje" -> NodeType.Sum
                        | "Oduzimanje" -> NodeType.Subtract
                        | "Množenje" -> NodeType.Multiply
                        | "Dijeljenje" -> NodeType.Divide
                        | "Korijen" -> NodeType.Sqrt
                        | _ -> NodeType.Input
                    Inputs = inputIds
                    Value = None
                }

                let allNodes = inputNodes @ [ virtualNode ]

                try
                    let result = calculateNodeValue allNodes virtualNode.Id
                    match result |> AVal.force with
                    | Some v ->
                        this.CalculationResult <- v
                        let inputLabels = inputNodes |> List.map (fun n -> (n.Id, n.Name))
                        calculationPerformed.Trigger(inputLabels, selectedOperation, v)
                    | None ->
                        this.CalculationResult <- nan

                    for node in selectedNodes do
                        node.IsSelected <- false
                    selectedNodes.Clear()
                    this.SelectedOperation <- "Odaberi"

                with
                | :? System.ArgumentException ->
                    this.CalculationResult <- nan
        )

    member this.AddInputCommand : ICommand =
        RelayCommand(fun () ->
            match System.Double.TryParse(newValue) with
            | true, v ->
                let node = RuntimeNode(System.Guid.NewGuid().GetHashCode(), $"Input {nodes.Count + 1}", AVal.constant (Some v))
                nodes.Add(node)
                this.NewValue <- ""
            | _ ->
                Debug.WriteLine("Invalid input (not a number)")
        )

    member this.AddOperationCommand : ICommand =
        RelayCommand(fun () ->
            let existingOps =
                nodes
                |> Seq.filter (fun n ->
                    match n.Name with
                    | "Zbrajanje" | "Oduzimanje" | "Množenje" | "Dijeljenje" | "Korijen" -> true
                    | _ -> false)
                |> Seq.toList

            for op in existingOps do
                nodes.Remove(op) |> ignore

            let node = RuntimeNode(System.Guid.NewGuid().GetHashCode(), selectedOperation, AVal.constant None)
            nodes.Add(node)
        )

    member this.ClearAllCommand : ICommand =
        RelayCommand(fun () ->
            nodes.Clear()
            selectedNodes.Clear()
            this.CalculationResult <- 0.0
            this.SelectedOperation <- "Odaberi"
            this.NewValue <- ""
            clearAllRequested.Trigger() // ✅ obavijestimo view da treba počistiti canvas
        )
