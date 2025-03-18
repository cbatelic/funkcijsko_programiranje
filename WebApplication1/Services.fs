module Services

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

type Variable(name: string, initialValue: float) =
    let mutable value = initialValue
    member this.Name = name
    member this.Value
        with get() = value
        and set(v) = value <- v

type IPropagator =
    abstract member CanPropagate: Variable -> bool
    abstract member Update: Variable -> unit

type TemperaturePressurePropagator() =
    interface IPropagator with
        member this.CanPropagate(variable: Variable) =
            variable.Name = "Temperature"

        member this.Update(variable: Variable) =
            if variable.Name = "Temperature" && variable.Value > 80.0 then
                let newPressure = variable.Value * 0.1
                printfn "Temperature exceeded 80°C, updating Pressure to %f" newPressure
                let pressureVariable = new Variable("Pressure", newPressure)
                printfn "Pressure updated to %f" pressureVariable.Value

type PropagatorService() =
    let variables = System.Collections.Generic.Dictionary<string, Variable>()
    let propagators = ref []
    let logs = ref [] 
    
    let history = ref []
    
    member this.RegisterVariable(name: string, initialValue: float) =
        let variable = Variable(name, initialValue)
        variables.Add(name, variable)
    
    member this.RegisterPropagator(propagator: IPropagator) =
        propagators := propagator :: !propagators  
    
    member this.UpdateVariable(name: string, newValue: float) =
        if variables.ContainsKey(name) then
            let variable = variables.[name]
            variable.Value <- newValue

            history := (name, variable.Value) :: !history  

            !propagators |> List.iter (fun propagator ->
                if propagator.CanPropagate(variable) then
                    propagator.Update(variable)
            )

            logs := sprintf "Variable %s updated to %f" name newValue :: !logs

    member this.GetVariable(name: string) =
        if variables.ContainsKey(name) then
            variables.[name].Value
        else
            0.0

    member this.GetLogs() =
        !logs

    member this.GetHistory() =
        !history 
