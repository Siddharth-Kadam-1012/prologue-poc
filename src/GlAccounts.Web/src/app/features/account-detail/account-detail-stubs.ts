import { Component, Input } from '@angular/core';
@Component({ selector: 'app-attributes-panel', template: '<p>Attributes panel — coming in Phase 17</p>' })
export class AttributesPanelStubComponent { @Input() accountId = ''; }

@Component({ selector: 'app-account-groups', template: '<p>Account groups — coming in Phase 18</p>' })
export class AccountGroupsStubComponent { @Input() accountId = ''; }

@Component({ selector: 'app-period-summary', template: '<p>Period summary — coming in Phase 18</p>' })
export class PeriodSummaryStubComponent { @Input() accountId = ''; }

@Component({ selector: 'app-transaction-list', template: '<p>Transaction history — coming in Phase 18</p>' })
export class TransactionListStubComponent { @Input() accountId = ''; }
