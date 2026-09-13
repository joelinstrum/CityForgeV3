using System;
using UnityEngine;
using UnityEngine.UIElements;
using CityForgeV3.World;
namespace CityForgeV3.UI
{
 public sealed partial class CityForgeApp
 {
  private void AddStoneLibraryCard(VisualElement grid,string id,string label,Action select)
  {
   var card=new VisualElement {name="flora-card-"+id};
   card.AddToClassList("road-material-card");
   var preview=new Image { sprite=StoneFloraCatalog.CreatePreviewSprite(id), scaleMode=ScaleMode.ScaleToFit };preview.AddToClassList("road-material-swatch");
   card.Add(preview);
   var button=CfButton.Create(label.ToUpperInvariant(),select,true,"quiet");
   button.name="flora-select-"+id;card.Add(button);grid.Add(card);
  }
 }
}
