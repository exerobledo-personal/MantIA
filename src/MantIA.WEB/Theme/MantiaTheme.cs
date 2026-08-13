using MudBlazor;

namespace MantIA.WEB.Theme;

public static class MantiaTheme
{
    public static readonly MudTheme Actual = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0B6FA4",
            PrimaryContrastText = "#FFFFFF",
            Secondary = "#EE7A22",
            SecondaryContrastText = "#FFFFFF",
            Tertiary = "#00909B",
            Info = "#2E90C4",
            Success = "#1E9E62",
            Warning = "#DE9021",
            Error = "#D14343",
            Dark = "#0F2233",

            Background = "#F1F5F9",
            BackgroundGray = "#E7EDF3",
            Surface = "#FFFFFF",

            AppbarBackground = "#FFFFFF",
            AppbarText = "#0F2233",

            DrawerBackground = "#0F2233",
            DrawerText = "#CFDCE6",
            DrawerIcon = "#8FA8BC",

            TextPrimary = "#14293C",
            TextSecondary = "#5B7286",
            TextDisabled = "#9AAEBE",

            ActionDefault = "#5B7286",
            ActionDisabled = "#B4C3CF",
            ActionDisabledBackground = "#E3EAF0",

            Divider = "#DEE7EE",
            DividerLight = "#EDF2F6",
            TableLines = "#E3EAF0",
            LinesDefault = "#DEE7EE",
            LinesInputs = "#C4D2DD",

            GrayLight = "#EDF2F6",
            GrayLighter = "#F5F8FA"
        },

        PaletteDark = new PaletteDark
        {
            Primary = "#3D9FD1",
            PrimaryContrastText = "#08151E",
            Secondary = "#F59042",
            Tertiary = "#26B3BE",
            Info = "#54A9D6",
            Success = "#35B87A",
            Warning = "#E9A845",
            Error = "#E36A6A",
            Dark = "#060D13",

            Background = "#0E1820",
            BackgroundGray = "#0A131A",
            Surface = "#15232E",

            AppbarBackground = "#15232E",
            AppbarText = "#E6EDF3",

            DrawerBackground = "#0A131A",
            DrawerText = "#C3D3DF",
            DrawerIcon = "#7E96A8",

            TextPrimary = "#E6EDF3",
            TextSecondary = "#9FB3C2",
            TextDisabled = "#63798A",

            Divider = "#22333F",
            DividerLight = "#1B2A35",
            TableLines = "#22333F",
            LinesDefault = "#22333F",
            LinesInputs = "#2E4250"
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px",
            DrawerWidthLeft = "278px",
            AppbarHeight = "74px"
        }
    };
}
