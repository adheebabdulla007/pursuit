# AUTH-2: Refresh-token concurrency

Refresh rotation and logout acquire a SQL Server update lock on the token owner's
Users row inside a transaction. The owner lookup uses the immutable token hash;
token and user state are reloaded after acquiring the lock. Locks are held until
commit or rollback and coordinate separate API processes, without a schema change.

Creating a replacement and revoking/linking its predecessor now commit together.
Exceptions and cancellation roll back both writes. Request scopes must be disposed
after a failed transaction because their tracked entities may reflect rolled-back writes.

The lock covers all refresh tokens for the user. This preserves the existing strict
replay policy: a reused token revokes every refresh token for that user. Revocation
commits before the service throws the authentication error. Concurrent use of one
token yields one successful rotation and rejected duplicates; the duplicates also
invalidate the winner's refresh token, requiring login again. No grace period or
cross-tab refresh coordination is introduced here. Existing access tokens retain
their usual expiration behavior.

Logout participates in the same lock and revokes the supplied token. It does not
revoke descendants when the supplied token was already rotated; logout across an
in-flight browser refresh remains a separate client/session lifecycle concern.

Integration coverage uses real SQL Server, separate scopes for concurrent requests,
and fresh scopes to inspect committed state. It checks one successor under concurrent
refresh, replay revocation, and rollback after both replacement and predecessor writes.
