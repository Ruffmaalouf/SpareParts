namespace SpareParts.Desktop.Abstractions.UsedCars;

public interface IUsedCarWorkspaceService
{
    void OpenGallery(UsedCarWorkspaceRequest request);
    void OpenParts(UsedCarWorkspaceRequest request);
    void OpenListingPackage(UsedCarWorkspaceRequest request);
}
