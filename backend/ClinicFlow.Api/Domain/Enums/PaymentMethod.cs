namespace ClinicFlow.Api.Domain.Enums;

/// <summary>
/// How a payment was collected. Deliberately minimal (Cash/Other) since
/// this build has no payment gateway integration — billing is manual
/// entry only, per the BRD.
/// </summary>
public enum PaymentMethod
{
    Cash,
    Other
}