namespace Advize_PlantEverything.Framework
{
	internal struct ExtraResource
	{
		public string prefabName;
		public string resourceName;
		public int resourceCost;
		public bool groundOnly;
		
		public ExtraResource(string prefabName, string resourceName, int resourceCost = 1, bool groundOnly = true)
		{
			this.prefabName = prefabName;
			this.resourceName = resourceName;
			this.resourceCost = resourceCost;
			this.groundOnly = groundOnly;
		}
		
		internal bool IsValid() => !(prefabName == default || prefabName.StartsWith("PE_Fake") || resourceName == default || resourceCost == default);
	}
}
