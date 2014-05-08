using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Ecosim
{
	public class GridTextureSettings
	{
		public readonly bool showZero;
		public readonly int offset;
		public readonly int elementsPerRow;
		public readonly Material material;
		public readonly bool activeShowZero;
		public readonly Material activeMaterial;

		public GridTextureSettings(bool showZero, int offset, int elementsPerRow, string materialName) {
			this.showZero = showZero;
			this.activeShowZero = showZero;
			this.offset = offset;
			this.elementsPerRow = elementsPerRow;
			material = EcoTerrainElements.GetMaterial(materialName);
			activeMaterial = material;
		}
		
		public GridTextureSettings(bool showZero, int offset, int elementsPerRow, string materialName, bool activeShowZero, string activeMaterialName) {
			this.showZero = showZero;
			this.activeShowZero = activeShowZero;
			this.offset = offset;
			this.elementsPerRow = elementsPerRow;
			material = EcoTerrainElements.GetMaterial(materialName);
			if (activeMaterialName != null) {
				activeMaterial = EcoTerrainElements.GetMaterial(activeMaterialName);
			}
			else {
				activeMaterial = material;
			}
		}

		public GridTextureSettings(bool showZero, int offset, int elementsPerRow, Material material) {
			this.showZero = showZero;
			this.activeShowZero = showZero;
			this.offset = offset;
			this.elementsPerRow = elementsPerRow;
			this.material = material;
			this.activeMaterial = material;
		}
		
		public GridTextureSettings(bool showZero, int offset, int elementsPerRow, Material material, bool activeShowZero, Material activeMaterial) {
			this.showZero = showZero;
			this.activeShowZero = activeShowZero;
			this.offset = offset;
			this.elementsPerRow = elementsPerRow;
			this.material = material;
			if (activeMaterial != null) {
				this.activeMaterial = activeMaterial;
			}
			else {
				this.activeMaterial = material;
			}
		}
		
	}
}