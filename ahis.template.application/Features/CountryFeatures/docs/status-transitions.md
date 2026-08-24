# Country Status Transitions

```text
Create -> IsActive=true, IsDelete=false
Update -> IsActive may become true or false; IsDelete is unchanged
Delete -> IsActive=false, IsDelete=true
```

There is no Country restore endpoint. Generic repository restore support exists but is not a confirmed Country use case.
