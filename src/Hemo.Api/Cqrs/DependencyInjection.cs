using Hemo.Application.Hemovigilance;
using Hemo.Application.Blood;
using Hemo.Application.Clinical;
using Hemo.Application.Authentication;
using Hemo.Application.Users;
using Hemo.Application.Organization;
using Hemo.Application.Cqrs;
using Hemo.Application.Patients;

namespace Hemo.Api.Cqrs;

/// <summary>Explicit composition root: every use case has one dedicated handler.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddHemoCqrs(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<ListAdmissionsQuery, AdmissionOperation<AdmissionPage>>, ListAdmissionsHandler>();
        services.AddScoped<IRequestHandler<GetAdmissionQuery, AdmissionOperation<AdmissionView>>, GetAdmissionHandler>();
        services.AddScoped<IRequestHandler<CreateAdmissionCommand, AdmissionOperation<AdmissionView>>, CreateAdmissionHandler>();
        services.AddScoped<IRequestHandler<UpdatePatientCommand, AdmissionOperation<AdmissionView>>, UpdatePatientHandler>();
        services.AddScoped<IRequestHandler<IssueWristbandCommand, AdmissionOperation<WristbandView>>, IssueWristbandHandler>();
        services.AddScoped<IRequestHandler<VerifyIdentityCommand, AdmissionOperation<IdentityVerificationView>>, VerifyIdentityHandler>();
        services.AddScoped<IRequestHandler<ListAllFacilitiesQuery, IReadOnlyList<OrgItem>>, ListAllFacilitiesHandler>();
        services.AddScoped<IRequestHandler<ListScopedFacilitiesQuery, IReadOnlyList<OrgItem>>, ListScopedFacilitiesHandler>();
        services.AddScoped<IRequestHandler<ListDepartmentsQuery, IReadOnlyList<OrgItem>>, ListDepartmentsHandler>();
        services.AddScoped<IRequestHandler<ListAvailableBedsQuery, Outcome<IReadOnlyList<OrgItem>>>, ListAvailableBedsHandler>();
        services.AddScoped<IRequestHandler<CreateFacilityCommand, Outcome<OrgItem>>, CreateFacilityHandler>();
        services.AddScoped<IRequestHandler<CreateDepartmentCommand, Outcome<OrgItem>>, CreateDepartmentHandler>();
        services.AddScoped<IRequestHandler<CreateBedCommand, Outcome<OrgItem>>, CreateBedHandler>();
        services.AddScoped<IRequestHandler<ListManagedUsersQuery, Outcome<IReadOnlyList<ManagedUser>>>, ListManagedUsersHandler>();
        services.AddScoped<IRequestHandler<ListManagedRolesQuery, IReadOnlyList<ManagedRole>>, ListManagedRolesHandler>();
        services.AddScoped<IRequestHandler<CreateManagedUserCommand, Outcome<ManagedUser>>, CreateManagedUserHandler>();
        services.AddScoped<IRequestHandler<GrantManagedRoleCommand, Outcome<GrantReceipt>>, GrantManagedRoleHandler>();
        services.AddScoped<IRequestHandler<DeactivateManagedUserCommand, Outcome<bool>>, DeactivateManagedUserHandler>();
        services.AddScoped<IRequestHandler<AuthenticateCommand, Outcome<SignInTicket>>, AuthenticateHandler>();
        services.AddScoped<IRequestHandler<GetSessionQuery, Outcome<SessionView>>, GetSessionHandler>();
        services.AddScoped<IRequestHandler<ValidateSessionQuery, bool>, ValidateSessionHandler>();
        services.AddScoped<IRequestHandler<RevokeSessionCommand, Outcome<bool>>, RevokeSessionHandler>();
        services.AddScoped<IRequestHandler<HasGlobalPermissionQuery, bool>, HasGlobalPermissionHandler>();
        services.AddScoped<IRequestHandler<BootstrapSystemAdministratorCommand, Outcome<bool>>, BootstrapSystemAdministratorHandler>();
        services.AddScoped<IRequestHandler<ListProductTypesQuery, Outcome<IReadOnlyList<ProductTypeOption>>>, ListProductTypesHandler>();
        services.AddScoped<IRequestHandler<ListBloodRequestsQuery, Outcome<BloodRequestPage>>, ListBloodRequestsHandler>();
        services.AddScoped<IRequestHandler<GetBloodRequestQuery, Outcome<BloodRequestView>>, GetBloodRequestHandler>();
        services.AddScoped<IRequestHandler<SubmitBloodRequestCommand, Outcome<BloodRequestView>>, SubmitBloodRequestHandler>();
        services.AddScoped<IRequestHandler<ReviewBloodRequestCommand, Outcome<BloodRequestView>>, ReviewBloodRequestHandler>();
        services.AddScoped<IRequestHandler<ListStockQuery,Outcome<IReadOnlyList<InventoryUnitView>>>,ListStockHandler>();
        services.AddScoped<IRequestHandler<ListBloodGroupsQuery,Outcome<IReadOnlyList<BloodGroupOption>>>,ListBloodGroupsHandler>();
        services.AddScoped<IRequestHandler<ReceiveStockCommand,Outcome<InventoryUnitView>>,ReceiveStockHandler>();
        services.AddScoped<IRequestHandler<ReserveStockCommand,Outcome<long>>,ReserveStockHandler>();
        services.AddScoped<IRequestHandler<RecordCrossmatchCommand,Outcome<CrossmatchView>>,RecordCrossmatchHandler>();
        services.AddScoped<IRequestHandler<FinalizeCrossmatchCommand,Outcome<CrossmatchView>>,FinalizeCrossmatchHandler>();
        services.AddScoped<IRequestHandler<ListBankWorkQuery,Outcome<IReadOnlyList<BankWorkItem>>>,ListBankWorkHandler>();
        services.AddScoped<IRequestHandler<ListIssuesQuery,Outcome<IReadOnlyList<IssueView>>>,ListIssuesHandler>();
        services.AddScoped<IRequestHandler<IssueUnitCommand,Outcome<IssueView>>,IssueUnitHandler>();
        services.AddScoped<IRequestHandler<DispatchIssueCommand,Outcome<DispatchView>>,DispatchIssueHandler>();
        services.AddScoped<IRequestHandler<ReceiveIssueCommand,Outcome<IssueView>>,ReceiveIssueHandler>();
        services.AddScoped<IRequestHandler<ReturnIssueCommand,Outcome<ReturnView>>,ReturnIssueHandler>();
        services.AddScoped<IRequestHandler<ReviewReturnCommand,Outcome<ReturnView>>,ReviewReturnHandler>();
        services.AddScoped<IRequestHandler<ListReturnsQuery,Outcome<IReadOnlyList<ReturnView>>>,ListReturnsHandler>();
        services.AddScoped<IRequestHandler<ListTransfusionsQuery,Outcome<IReadOnlyList<TransfusionView>>>,ListTransfusionsHandler>();
        services.AddScoped<IRequestHandler<GetTransfusionQuery,Outcome<TransfusionDetails>>,GetTransfusionHandler>();
        services.AddScoped<IRequestHandler<PrepareTransfusionCommand,Outcome<TransfusionView>>,PrepareTransfusionHandler>();
        services.AddScoped<IRequestHandler<RecordChecklistCommand,Outcome<TransfusionView>>,RecordChecklistHandler>();
        services.AddScoped<IRequestHandler<RecordVitalCommand,Outcome<VitalView>>,RecordVitalHandler>();
        services.AddScoped<IRequestHandler<StartTransfusionCommand,Outcome<TransfusionView>>,StartTransfusionHandler>();
        services.AddScoped<IRequestHandler<StopTransfusionCommand,Outcome<TransfusionView>>,StopTransfusionHandler>();
        services.AddScoped<IRequestHandler<CompleteTransfusionCommand,Outcome<TransfusionView>>,CompleteTransfusionHandler>();
        services.AddScoped<IRequestHandler<SignoffTransfusionCommand,Outcome<TransfusionView>>,SignoffTransfusionHandler>();
        services.AddScoped<IRequestHandler<ListReactionsQuery,Outcome<IReadOnlyList<ReactionView>>>,ListReactionsHandler>();
        services.AddScoped<IRequestHandler<GetReactionQuery,Outcome<ReactionDetails>>,GetReactionHandler>();
        services.AddScoped<IRequestHandler<ReportReactionCommand,Outcome<ReactionView>>,ReportReactionHandler>();
        services.AddScoped<IRequestHandler<AssessReactionCommand,Outcome<ReactionView>>,AssessReactionHandler>();
        services.AddScoped<IRequestHandler<InvestigateReactionCommand,Outcome<ReactionView>>,InvestigateReactionHandler>();
        services.AddScoped<IRequestHandler<ReviewReactionCommand,Outcome<ReactionView>>,ReviewReactionHandler>();
        services.AddScoped<IRequestHandler<OpenRcaCommand,Outcome<RcaView>>,OpenRcaHandler>();
        services.AddScoped<IRequestHandler<AddRcaCauseCommand,Outcome<RcaView>>,AddRcaCauseHandler>();
        services.AddScoped<IRequestHandler<AssignCorrectiveActionCommand,Outcome<CorrectiveActionView>>,AssignCorrectiveActionHandler>();
        services.AddScoped<IRequestHandler<CompleteCorrectiveActionCommand,Outcome<CorrectiveActionView>>,CompleteCorrectiveActionHandler>();
        services.AddScoped<IRequestHandler<VerifyCorrectiveActionCommand,Outcome<CorrectiveActionView>>,VerifyCorrectiveActionHandler>();
        services.AddScoped<IRequestHandler<CloseRcaCommand,Outcome<RcaView>>,CloseRcaHandler>();
        return services;
    }
}
