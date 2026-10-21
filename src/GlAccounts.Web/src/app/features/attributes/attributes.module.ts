import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { AttributesPanelComponent } from './attributes-panel.component';

@NgModule({
  imports: [CommonModule, FormsModule, HttpClientModule],
  declarations: [AttributesPanelComponent],
  exports: [AttributesPanelComponent]
})
export class AttributesModule {}
