module AvaloniaApplication1.Models.Node

type NodeType =
    | Input
    | Sum
    | Multiply
    | Subtract
    | Divide
    | Output
    | Sqrt

type Node = {
    Id: int
    Name: string
    NodeType: NodeType
    Inputs: int list
    Value: float option
}
