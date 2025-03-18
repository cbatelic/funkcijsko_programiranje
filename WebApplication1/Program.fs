module WebApplication1

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Http
open Services
open System.Threading.Tasks
open System.Collections.Generic
open System.Threading  
open System 

type VariableDto = {
    Name: string
    Value: float
}

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    let app = builder.Build()

    let propagatorService = PropagatorService()

    propagatorService.RegisterVariable("Temperature", 20.0)  
    propagatorService.RegisterVariable("Pressure", 101.3)    

    let temperaturePressurePropagator = TemperaturePressurePropagator()
    propagatorService.RegisterPropagator(temperaturePressurePropagator)

    app.MapGet("/", RequestDelegate(fun context -> 
        task {
            printfn "Root endpoint hit: Returning Hello World!"
            do! context.Response.WriteAsync("Hello World!")
        }
    )) |> ignore

    app.MapGet("/api/logs", RequestDelegate(fun context -> 
        task {
            let logs = propagatorService.GetLogs()

            let logsResponse = 
                if List.isEmpty logs then 
                    "No logs yet."
                else
                    System.String.Join("\n", logs)
            do! context.Response.WriteAsync(logsResponse)
        }
    )) |> ignore

    app.MapGet("/api/history", RequestDelegate(fun context -> 
        task {
            let history = propagatorService.GetHistory()

            let historyJson = 
                history |> List.map (fun (name, value) -> sprintf "{ \"name\": \"%s\", \"value\": %f }" name value)
                        |> String.concat ", "

            let response = sprintf "[ %s ]" historyJson

            do! context.Response.WriteAsync(response)
        }
    )) |> ignore

    app.MapPost("/api/updateTemperature", RequestDelegate(fun context -> 
        task {
            let! newTemperature = context.Request.ReadFromJsonAsync<float>()

            propagatorService.UpdateVariable("Temperature", newTemperature)

            do! context.Response.WriteAsync(sprintf "Updated Temperature to %f" newTemperature)
        }
    )) |> ignore

    let timerCallback (state: obj) =
        let random = Random()
        
        let newTemperature = (float (random.Next(20, 100)))
        propagatorService.UpdateVariable("Temperature", newTemperature)

        let newPressure = (float (random.Next(100, 200)))
        propagatorService.UpdateVariable("Pressure", newPressure)

        printfn "Updated Temperature to: %f" newTemperature
        printfn "Updated Pressure to: %f" newPressure
        ()

    let timerCallbackDelegate = new TimerCallback(timerCallback)
    let timer = new Timer(timerCallbackDelegate, null, 0, 5000)  

    app.Run()
    0
