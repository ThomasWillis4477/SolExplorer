using IsoMauiEngine.Diagnostics;

namespace IsoMauiEngine.Views;

internal static class InfoPageFactory
{
	public static ContentPage Create(string title, string description)
	{
		var content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = 20,
				Spacing = 12,
				Children =
				{
					new Label
					{
						Text = title,
						FontSize = 22,
						FontAttributes = FontAttributes.Bold,
						TextColor = Colors.White,
					},
					new Label
					{
						Text = description,
						FontSize = 14,
						LineBreakMode = LineBreakMode.WordWrap,
						TextColor = Colors.White,
					},
					new BoxView { HeightRequest = 1, Color = Colors.Gray, Opacity = 0.4 },
					new Label
					{
						Text = "Tip: Use the left flyout menu to switch sections. If you don’t see it, click the hamburger icon (top-left) or widen the window.",
						FontSize = 12,
						Opacity = 0.8,
						TextColor = Colors.White,
					},
				}
			}
		};

		var overlay = new Border
		{
			Margin = 12,
			Padding = 10,
			HorizontalOptions = LayoutOptions.Start,
			VerticalOptions = LayoutOptions.Start,
			BackgroundColor = Color.FromArgb("#AA000000"),
			Stroke = Color.FromArgb("#33000000"),
			StrokeThickness = 1,
			Content = new VerticalStackLayout
			{
				Spacing = 2,
				Children =
				{
					new Label { Text = "Route debug", FontSize = 12, TextColor = Colors.White, Opacity = 0.9 },
					new Label
					{
						FontSize = 12,
						TextColor = Colors.White,
						FormattedText = new FormattedString
						{
							Spans =
							{
								new Span { Text = "Current: " },
								new Span { Text = RouteDebugState.Instance.CurrentLocation },
							}
						}
					},
					new Label
					{
						FontSize = 12,
						TextColor = Colors.White,
						FormattedText = new FormattedString
						{
							Spans =
							{
								new Span { Text = "Navigating: " },
								new Span { Text = RouteDebugState.Instance.LastNavigating },
							}
						}
					},
					new Label
					{
						FontSize = 12,
						TextColor = Colors.White,
						FormattedText = new FormattedString
						{
							Spans =
							{
								new Span { Text = "Navigated: " },
								new Span { Text = RouteDebugState.Instance.LastNavigated },
							}
						}
					},
				}
			}
		};

		// Keep overlay text updated.
		overlay.BindingContext = RouteDebugState.Instance;
		((Label)((VerticalStackLayout)overlay.Content).Children[1]).SetBinding(Label.TextProperty, nameof(RouteDebugState.CurrentLocation), stringFormat: "Current: {0}");
		((Label)((VerticalStackLayout)overlay.Content).Children[2]).SetBinding(Label.TextProperty, nameof(RouteDebugState.LastNavigating), stringFormat: "Navigating: {0}");
		((Label)((VerticalStackLayout)overlay.Content).Children[3]).SetBinding(Label.TextProperty, nameof(RouteDebugState.LastNavigated), stringFormat: "Navigated: {0}");

		var root = new Grid();
		root.Children.Add(content);
		root.Children.Add(overlay);

		return new ContentPage
		{
			Title = title,
			BackgroundColor = Color.FromArgb("#0B0F14"),
			Content = root
		};
	}
}

public sealed class ShellContentInfoPage : ContentPage
{
	public ShellContentInfoPage()
		: base()
	{
		var body =
			"ShellContent is the leaf node in Shell navigation. It hosts ONE Page (via ContentTemplate) and is what you actually navigate to.\n\n" +
			"Use it when:\n" +
			"- You want a navigable page endpoint (e.g., MainPage, SettingsPage).\n" +
			"- You want to set a title/icon for a page inside a tab or flyout.\n\n" +
			"In this demo: each menu item or tab ultimately points to a ShellContent.";

		var page = InfoPageFactory.Create("ShellContent", body);
		Content = page.Content;
		Title = page.Title;
	}
}

public sealed class FlyoutItemInfoPage : ContentPage
{
	public FlyoutItemInfoPage()
	{
		var body =
			"FlyoutItem creates a top-level entry in the Shell flyout (hamburger menu). It’s a container for one or more sections/pages.\n\n" +
			"Use it when:\n" +
			"- You want major app areas (Explorer, Library, Settings).\n" +
			"- You want the left navigation menu to show distinct destinations.";

		var page = InfoPageFactory.Create("FlyoutItem", body);
		Content = page.Content;
		Title = page.Title;
	}
}

public sealed class ShellSectionInfoPageA : ContentPage
{
	public ShellSectionInfoPageA()
	{
		var body =
			"ShellSection is a subsection within a FlyoutItem (or ShellItem). A FlyoutItem can contain multiple ShellSections.\n\n" +
			"On desktop, multiple ShellSections often appear as top tabs within that FlyoutItem.\n\n" +
			"Use it when:\n" +
			"- You want grouped sub-areas inside one flyout entry.\n" +
			"- You want tab-like subsections under a single major area.";

		var page = InfoPageFactory.Create("ShellSection (A)", body);
		Content = page.Content;
		Title = page.Title;
	}
}

public sealed class ShellSectionInfoPageB : ContentPage
{
	public ShellSectionInfoPageB()
	{
		var body =
			"This is a second ShellSection under the same FlyoutItem.\n\n" +
			"Switch between Section A and Section B to see how sections are presented by Shell on Windows.";

		var page = InfoPageFactory.Create("ShellSection (B)", body);
		Content = page.Content;
		Title = page.Title;
	}
}

public sealed class TabBarInfoPage1 : ContentPage
{
	public TabBarInfoPage1()
	{
		var body =
			"TabBar is a top-level container for tab navigation. It contains multiple ShellContent entries (tabs).\n\n" +
			"Use it when:\n" +
			"- You want fast switching between peer screens (e.g., Home / Search / Profile).\n" +
			"- You want a tabbed UI instead of (or in addition to) a flyout.";

		var page = InfoPageFactory.Create("TabBar (Tab 1)", body);
		Content = page.Content;
		Title = page.Title;
	}
}

public sealed class TabBarInfoPage2 : ContentPage
{
	public TabBarInfoPage2()
	{
		var body =
			"This is Tab 2 inside the TabBar.\n\n" +
			"Each tab is a ShellContent. TabBar is the container that organizes them.";

		var page = InfoPageFactory.Create("TabBar (Tab 2)", body);
		Content = page.Content;
		Title = page.Title;
	}
}
