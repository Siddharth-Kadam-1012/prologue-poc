import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HttpClientModule } from '@angular/common/http';
import { AccountDetailComponent } from './account-detail.component';
import { AccountActionBarComponent } from './account-action-bar.component';
import { AttributesModule } from '../attributes/attributes.module';
import { AccountGroupsModule } from '../account-groups/account-groups.module';
import { PeriodSummaryModule } from '../period-summary/period-summary.module';
import { TransactionsModule } from '../transactions/transactions.module';

@NgModule({
  imports: [
    CommonModule,
    HttpClientModule,
    RouterModule.forChild([{ path: '', component: AccountDetailComponent }]),
    AttributesModule,
    AccountGroupsModule,
    PeriodSummaryModule,
    TransactionsModule
  ],
  declarations: [
    AccountDetailComponent,
    AccountActionBarComponent
  ]
})
export class AccountDetailModule {}
