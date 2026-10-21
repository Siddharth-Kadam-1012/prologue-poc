import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { TransactionListComponent } from './transaction-list.component';

@NgModule({
  imports: [CommonModule, FormsModule, HttpClientModule],
  declarations: [TransactionListComponent],
  exports: [TransactionListComponent]
})
export class TransactionsModule {}
