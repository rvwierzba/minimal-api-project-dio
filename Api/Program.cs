using minimal_api;

public class Program
{  

    public static void Main(string[] args)
    {
      CreateHostBuilder(args).Build().Run();
    }
  
  public static IHostBuilder CreateHostBuilder(string[] args){
    return Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder => {
      webBuilder.UseStartup<Startup>();
    });
  }


}




/*#region Builder
  var builder = WebApplication.CreateBuilder(args);

    

   

    var app = builder.Build();
#endregion



#region App


app.Run();

#endregion
*/