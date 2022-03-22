using OpenSee.Common;

namespace OpenSee;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("Lato-Regular.ttf", "Lato");
				fonts.AddFont("tabler-icons.ttf", "TablerIcons");
				fonts.AddFont("Pacifico-Regular.ttf", "Pacifico");
			})
			.Services.AddSingleton<OpenSeeService>();

		return builder.Build();
	}
}
