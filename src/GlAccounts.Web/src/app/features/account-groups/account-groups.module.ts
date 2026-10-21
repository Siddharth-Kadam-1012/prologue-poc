import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { AccountGroupsComponent } from './account-groups.component';

@NgModule({
  imports: [CommonModule, HttpClientModule],
  declarations: [AccountGroupsComponent],
  exports: [AccountGroupsComponent]
})
export class AccountGroupsModule {}
