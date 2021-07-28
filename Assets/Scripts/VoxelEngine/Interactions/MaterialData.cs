using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public struct MaterialData
{
    public byte materialId;
    public ushort advancedMaterialDataId;

    public MaterialData(byte materialId, ushort advancedMaterialDataId)
    {
        this.materialId = materialId;
        this.advancedMaterialDataId = advancedMaterialDataId;
    }
}
