import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ValidationSummaryComponent } from './validation-summary.component';

@NgModule({
  declarations: [ValidationSummaryComponent],
  imports: [CommonModule],
  exports: [ValidationSummaryComponent]
})
export class SharedModule {}
