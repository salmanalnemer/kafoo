using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class ExpandOrganizationEmploymentRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcceptAllDisabilities",
                table: "OpportunityRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AcceptedSpecializations",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 2500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdditionalAttachment",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminNotes",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApplicationEndDate",
                table: "OpportunityRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApplicationStartDate",
                table: "OpportunityRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssistiveTechnologies",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 3000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Benefits",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 3000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContractDurationMonths",
                table: "OpportunityRequests",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractType",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DepartmentName",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DrivingLicense",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EducationLevel",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmploymentType",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntroVideo",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobDescriptionFile",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobObjectives",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 3000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobReferenceNumber",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Languages",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 1500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinimumExperienceYears",
                table: "OpportunityRequests",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumGpa",
                table: "OpportunityRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpportunityImage",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationNotes",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PerformanceIndicators",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 3000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfessionalCertificates",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecruitmentOfficerEmail",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecruitmentOfficerName",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecruitmentOfficerPhone",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RemoteWorkAvailable",
                table: "OpportunityRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RequiredCourses",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Responsibilities",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalaryFrom",
                table: "OpportunityRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SalaryNegotiable",
                table: "OpportunityRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SalaryTo",
                table: "OpportunityRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScreeningQuestionsJson",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SupportEmployeeAvailable",
                table: "OpportunityRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalSkills",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 2500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkShift",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkplaceAccommodations",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 3000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WorkplaceCanBeModified",
                table: "OpportunityRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptAllDisabilities",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "AcceptedSpecializations",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "AdditionalAttachment",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "AdminNotes",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "ApplicationEndDate",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "ApplicationStartDate",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "AssistiveTechnologies",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "Benefits",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "ContractDurationMonths",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "ContractType",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "DepartmentName",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "DrivingLicense",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "EducationLevel",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "EmploymentType",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "IntroVideo",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "JobDescriptionFile",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "JobObjectives",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "JobReferenceNumber",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "Languages",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "MinimumExperienceYears",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "MinimumGpa",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "OpportunityImage",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "OrganizationNotes",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "PerformanceIndicators",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "ProfessionalCertificates",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "RecruitmentOfficerEmail",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "RecruitmentOfficerName",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "RecruitmentOfficerPhone",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "RemoteWorkAvailable",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "RequiredCourses",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "Responsibilities",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "SalaryFrom",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "SalaryNegotiable",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "SalaryTo",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "ScreeningQuestionsJson",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "SupportEmployeeAvailable",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "TechnicalSkills",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "WorkShift",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "WorkplaceAccommodations",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "WorkplaceCanBeModified",
                table: "OpportunityRequests");
        }
    }
}
