namespace SpareParts.Domain.Trust;

public enum FraudCategory
{
    WrongPartSent = 1,
    FakePhotos = 2,
    WorseConditionThanDescribed = 3,
    SellerDisappeared = 4,
    DuplicateListing = 5,
    ScamSuspicion = 6
}
