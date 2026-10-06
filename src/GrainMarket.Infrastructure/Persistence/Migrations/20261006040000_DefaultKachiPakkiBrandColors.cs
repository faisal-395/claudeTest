using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DefaultKachiPakkiBrandColors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Matches the legacy software's own look (green for Kachi, blue for Pakki) as the
            // default for these two scopes specifically — see
            // AppearanceSettingsService.NewDefaultsFor, which a brand-new row already picks up. An
            // install that ran before this change has its Kachi/Pakki rows already stored with the
            // old generic white defaults — flip those here too, same as
            // DefaultNavigationLayoutToTopMenu did for the Global row. Still fully overridable from
            // Setup > Appearance afterwards.
            migrationBuilder.Sql(@"
                UPDATE ""AppearanceSettings""
                SET ""HeadingBackgroundColor"" = '#C8E6C9', ""PanelColor"" = '#E8F5E9',
                    ""GridHeaderColor"" = '#A5D6A7', ""GridBackgroundColor"" = '#F1F8F2'
                WHERE ""Scope"" = 3;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""AppearanceSettings""
                SET ""HeadingBackgroundColor"" = '#BBDEFB', ""PanelColor"" = '#E3F2FD',
                    ""GridHeaderColor"" = '#90CAF9', ""GridBackgroundColor"" = '#F2F8FD'
                WHERE ""Scope"" = 4;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""AppearanceSettings""
                SET ""HeadingBackgroundColor"" = '#FFFFFF', ""PanelColor"" = '#FFFFFF',
                    ""GridHeaderColor"" = '#EEF4EE', ""GridBackgroundColor"" = '#FFFFFF'
                WHERE ""Scope"" IN (3, 4);
            ");
        }
    }
}
