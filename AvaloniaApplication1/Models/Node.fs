module AvaloniaApplication1.Models.Node

open FSharp.Data.Adaptive

type NodeType =
    | Input
    | Sum
    | Multiply
    | Output

type Node = {
    Id: int
    Name: string
    NodeType: NodeType
    Inputs: int list
    Value: float option
}


