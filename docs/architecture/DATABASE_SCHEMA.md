# Database Schema

Schema v1 includes Account, FinanceTransaction, TransactionSplit, Category, Tag, TransactionTag, Budget, BudgetPeriod, SavingsGoal, GoalContribution, RecurrenceRule, RecurrenceOccurrence, Attachment, AppSetting, AuditEntry, and BackupMetadata.

Transfers are paired FinanceTransaction rows sharing `TransferGroupId`. Recurrence occurrences have a unique `(RecurrenceRuleId, DueOn)` index to prevent restart duplicates.
