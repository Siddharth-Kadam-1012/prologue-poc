import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { PeriodSummaryComponent } from './period-summary.component';

@NgModule({
  imports: [CommonModule, FormsModule, HttpClientModule],
  declarations: [PeriodSummaryComponent],
  exports: [PeriodSummaryComponent]
})
export class PeriodSummaryModule {}
