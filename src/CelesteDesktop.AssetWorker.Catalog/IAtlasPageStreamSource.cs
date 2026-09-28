namespace CelesteDesktop.AssetWorker.Catalog;

public interface IAtlasPageStreamSource
{
    Stream? OpenPage(string logicalPath);
}
