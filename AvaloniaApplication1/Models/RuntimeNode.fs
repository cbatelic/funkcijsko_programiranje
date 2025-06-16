namespace AvaloniaApplication1.Models.RuntimeNode

open System.ComponentModel
open FSharp.Data.Adaptive
open System.Runtime.CompilerServices

[<AllowNullLiteral>]
type RuntimeNode(id: int, name: string, value: aval<float option>) =

    let mutable currentValue = value |> AVal.force
    let mutable isSelected = false

    let propertyChanged = new Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()

    do
        value.AddCallback(fun v ->
            currentValue <- v
            propertyChanged.Trigger(null, PropertyChangedEventArgs(nameof currentValue))
        ) |> ignore

    member this.Id = id
    member this.Name = name

    member this.Value
        with get() = currentValue
        and set(v) =
            currentValue <- v
            propertyChanged.Trigger(this, PropertyChangedEventArgs(nameof this.Value))

    member this.IsSelected
        with get() = isSelected
        and set(v) =
            if isSelected <> v then
                isSelected <- v
                propertyChanged.Trigger(this, PropertyChangedEventArgs(nameof this.IsSelected))

    member this.ForceUpdate(newValue: float option) =
        this.Value <- newValue

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member _.PropertyChanged = propertyChanged.Publish
