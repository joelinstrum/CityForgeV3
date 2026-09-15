using CityForgeV3.Buildings3D;
namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        private BuildingDoorController SelectedNativeDoor =>
            _selectedBuilding3DIndex >= 0 && _selectedBuilding3DIndex < _experimentalBuilding3DVisibleRoots.Count
            ? _experimentalBuilding3DVisibleRoots[_selectedBuilding3DIndex]?.GetComponentInChildren<BuildingDoorController>(true) : null;
        public bool SelectedBuildingHasDoor => SelectedNativeDoor != null;
        public bool SelectedBuildingDoorOpen => SelectedNativeDoor != null && SelectedNativeDoor.IsOpen;
        public bool ToggleSelectedBuildingDoor()
        {
            var door = SelectedNativeDoor;
            if (door == null || _selectedBuilding3DIndex >= _session.Data.Buildings3D.Count) return false;
            var placed = _session.Data.Buildings3D[_selectedBuilding3DIndex];
            placed.DoorOpen = !placed.DoorOpen; door.SetOpen(placed.DoorOpen); return true;
        }
    }
}
