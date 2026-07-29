# Auth Service

> This repository is part of the open source project ILM. You can find more information about the project, including the [contribution guide](https://github.com/OmniTrustILM/.github/blob/main/CONTRIBUTING.md), in the [OmniTrustILM](https://github.com/OmniTrustILM) organization.

`Auth` service is designed as a central service for managing access control to different resources and related actions and objects, and identify users based on identification token.

`Auth` service offers the following functionality:
- identification of user
- management of roles
- management of users and their roles membership
- management of resources and related actions
- management of role permissions for specific resources and related actions
- issuing authorization tokens

User can belong to multiple roles and permissions are merged in this case from all assigned roles.

> **Note**
> The authorization is performed by the OPA. The `Auth` service manages users, roles, and associated permissions that can be assigned to users.

## Authentication flow

Users can be identified based on identification token provided in this order:
1. X.509 certificate   
2. JSON ID
3. Username of system user

After successful identification, user details with its merged role permissions is returned. If none of the above specified data is present, user is identified as **anonymous** user with limited permissions.

### X.509 certificate

When identifying user with certificate, its content string is decoded and parsed.
Afterwards it is verified if it is valid and trusted.

Then, based on its fingerprint, it is mapped to user from database and return authentication response.
The certificate can be assigned to maximum of 1 user. 

### JSON ID

When identifying user with JSON ID, it is decoded and must conform to the required structure.
Username specified in the JSON ID is used as unique information based on which user is identified.

`Auth` service can be further configured to create user or role, if it does not exist.

### Username of system user

Username is used only for internal identification of system users to elevate permissions and perform actions that are otherwise subject to authorization. This is not exposed for the external systems.

## Authorization

This service does not evaluate permissions.
The authorization is controlled by the [Open Policy Agent](https://www.openpolicyagent.org/). For more information, refer to [ILM Documentation](https://docs.otilm.com/docs/certificate-key/concept-design/architecture/access-control/overview).

## Docker container

`Auth` service is provided as a Docker container. Use the `hub.omnitrustregistry.com/ilm/auth:tagname` to pull the required image from the repository. It can be configured using the following environment variables:

| Variable                    | Description                                                                                        | Required                                           | Default value |
|-----------------------------|----------------------------------------------------------------------------------------------------|----------------------------------------------------|---------------|
| `AUTH_DB_CONNECTION_STRING` | Connection string for database access                                                              | ![](https://img.shields.io/badge/-YES-success.svg) | `N/A`         |
| `AUTH_CREATE_UNKNOWN_USERS` | Unknown user with username specified in JSON authentication token will be created                  | ![](https://img.shields.io/badge/-NO-red.svg)      | `false`       |
| `AUTH_CREATE_UNKNOWN_ROLES` | Unknown role with name specified in JSON authentication token will be created and assigned to user | ![](https://img.shields.io/badge/-NO-red.svg)      | `false`       |
| `SYNC_POLICY` | `create-only` - create users and roles based on previous variables, changes in token not reflected in already existing user.<br />`sync-data` - user's properties and roles are synced with the user data present in token | ![](https://img.shields.io/badge/-NO-red.svg)      | `create-only`       |
