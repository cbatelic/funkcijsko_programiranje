namespace AvaloniaApplication1.ViewModels

open System.Collections.ObjectModel
open System.Windows.Input
open CommunityToolkit.Mvvm.ComponentModel
open CommunityToolkit.Mvvm.Input
open AvaloniaApplication1.Models.RuntimeNode
open AvaloniaApplication1.Models.Node
open AvaloniaApplication1.Logic.BidirectionalPropagation
open AvaloniaApplication1.Logic.PropagationService
open FSharp.Data.Adaptive
open System.Diagnostics

[<CLIMutable>]
type CalculationPerformedArgs = {
    Inputs: (int * string) list
    Operation: string
    Result: string
}

type MainWindowViewModel() =
    inherit ObservableObject()

    let nodes = ObservableCollection<RuntimeNode>()
    let selectedNodes = ResizeArray<RuntimeNode>()

    let operations = [ "Zbrajanje"; "Oduzimanje"; "Množenje"; "Dijeljenje"; "Korijen" ]
    let mutable selectedOperation = operations.Head
    let mutable newValue = ""
    let mutable expectedResult = ""
    let mutable calculationResult = 0.0

    let calculationPerformed = Event<CalculationPerformedArgs>()
    let clearAllRequested = Event<unit>()

    member this.CalculationPerformed = calculationPerformed.Publish
    member this.ClearAllRequested = clearAllRequested.Publish

    member this.Nodes = nodes

    member this.NewValue
        with get() = newValue
        and set v = this.SetProperty(&newValue, v) |> ignore

    member this.ExpectedResult
        with get() = expectedResult
        and set v = this.SetProperty(&expectedResult, v) |> ignore

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
            node.IsSelected <- not node.IsSelected
            if selectedNodes.Contains(node) then
                selectedNodes.Remove(node) |> ignore
            else
                selectedNodes.Add(node)
        )

    member this.AddInputCommand : ICommand =
        RelayCommand(fun () ->
            match System.Double.TryParse(newValue) with
            | true, v ->
                let node = RuntimeNode(System.Guid.NewGuid().GetHashCode(), v.ToString(), AVal.constant (Some v))
                nodes.Add(node)
                this.NewValue <- ""
            | _ -> Debug.WriteLine("⚠️ Neispravan unos")
        )

    member this.AddOperationCommand : ICommand =
        RelayCommand(fun () ->
            let inputNodes =
                selectedNodes
                |> Seq.map (fun n ->
                    {
                        Id = n.Id
                        Name = n.Name
                        NodeType = NodeType.Input
                        Inputs = []
                        Value =  n.Value
                    })
                |> Seq.toList

            let completedInputs =
                match inputNodes, System.Double.TryParse(expectedResult) with
                | inputs, (true, _) when inputs |> List.exists (fun i -> i.Value.IsNone) = false ->
                    let needed = 1 // Dodaj samo jedan unknown ako imamo očekivani rezultat
                    let generated =
                       [1..needed]
                    |> List.mapi (fun i _ ->
                        let id = System.Guid.NewGuid().GetHashCode()
                        let parsedResult = System.Double.TryParse(expectedResult) |> function | true, r -> r | _ -> 0.0
                        let sumKnown = inputs |> List.choose (fun i -> i.Value) |> List.sum
                        let unknownValue = parsedResult - sumKnown
                        { Id = id; Name = unknownValue.ToString(); NodeType = NodeType.Input; Inputs = []; Value = Some unknownValue })
                    inputs @ generated
                | _ -> inputNodes

            let nodeId = System.Guid.NewGuid().GetHashCode()

            let operationNode = {
                Id = nodeId
                Name = selectedOperation
                NodeType =
                    match selectedOperation with
                    | "Zbrajanje" -> NodeType.Sum
                    | "Oduzimanje" -> NodeType.Subtract
                    | "Množenje" -> NodeType.Multiply
                    | "Dijeljenje" -> NodeType.Divide
                    | "Korijen" -> NodeType.Sqrt
                    | _ -> NodeType.Input
                Inputs = completedInputs |> List.map (fun n -> n.Id)
                Value = None
            }

            let allNodes = completedInputs @ [ operationNode ]

            match propagate allNodes operationNode.Id with
            | Some result ->
                if not (nodes |> Seq.exists (fun n -> n.Id = operationNode.Id)) then
                    let newNode = RuntimeNode(operationNode.Id, selectedOperation, AVal.constant (Some result))
                    nodes.Add(newNode)

                this.CalculationResult <- result

                let args = {
                    Inputs = completedInputs |> List.map (fun n -> n.Id, n.Name)
                    Operation = selectedOperation
                    Result = result.ToString()
                }
                calculationPerformed.Trigger(args)

                for n in selectedNodes do
                    n.IsSelected <- false
                selectedNodes.Clear()

            | None ->
                match System.Double.TryParse(expectedResult) with
                | true, r ->
                    let reverseNode = { operationNode with Value = Some r }

                    let updatedInputs =
                        match selectedOperation with
                        | "Zbrajanje" -> tryBackwardSum allNodes reverseNode
                        | "Oduzimanje" -> tryBackwardSubtract allNodes reverseNode
                        | "Množenje" -> tryBackwardMultiply allNodes reverseNode
                        | "Dijeljenje" -> tryBackwardDivide allNodes reverseNode
                        | "Korijen" -> tryBackwardSqrt allNodes reverseNode
                        | _ -> []

                    for u in updatedInputs do
                        match nodes |> Seq.tryFind (fun n -> n.Id = u.Id) with
                        | Some runtime -> runtime.ForceUpdate(u.Value)
                        | None ->
                            let newInputNode = RuntimeNode(u.Id, u.Name, AVal.constant (u.Value))
                            nodes.Add(newInputNode)

                    this.CalculationResult <- r
                    let combined = (completedInputs @ updatedInputs)
                    let args = {
                        Inputs = combined |> List.distinctBy (fun n -> n.Id) |> List.map (fun n -> n.Id, n.Name)
                        Operation = selectedOperation
                        Result = r.ToString()
                    }
                    calculationPerformed.Trigger(args)

                    for n in selectedNodes do
                        n.IsSelected <- false
                    selectedNodes.Clear()

                | _ ->
                    this.CalculationResult <- nan
        )

    member this.ClearAllCommand : ICommand =
        RelayCommand(fun () ->
            nodes.Clear()
            selectedNodes.Clear()
            this.NewValue <- ""
            this.ExpectedResult <- ""
            this.CalculationResult <- 0.0
            this.SelectedOperation <- operations.Head
            clearAllRequested.Trigger()
        )
