# Organization API Documentation

## Overview

The Organization API provides endpoints for managing organizations in the Aevatar platform. These endpoints require authentication.

## Base URL

```
/api/organizations
```

## Authentication

All endpoints require Bearer token authentication.

## Endpoints

### List Organizations

Retrieve a list of organizations.

```http
GET /api/organizations
```

#### Query Parameters

Parameters defined in `GetOrganizationListDto`

#### Response

Returns a `ListResultDto<OrganizationDto>` containing the list of organizations.

### Get Organization

Retrieve a specific organization by ID.

```http
GET /api/organizations/{id}
```

#### Parameters

- `id` (path parameter): Organization GUID

#### Response

Returns an `OrganizationDto` object.

### Create Organization

Create a new organization.

```http
POST /api/organizations
```

#### Request Body

```json
{
  // CreateOrganizationDto properties
}
```

#### Response

Returns an `OrganizationDto` object.

### Update Organization

Update an existing organization.

```http
PUT /api/organizations/{id}
```

#### Parameters

- `id` (path parameter): Organization GUID

#### Request Body

```json
{
  // UpdateOrganizationDto properties
}
```

#### Response

Returns an `OrganizationDto` object.

### Delete Organization

Delete an existing organization.

```http
DELETE /api/organizations/{id}
```

#### Parameters

- `id` (path parameter): Organization GUID

### List Organization Members

Retrieve a list of organization members.

```http
GET /api/organizations/{organizationId}/members
```

#### Parameters

- `organizationId` (path parameter): Organization GUID
- Query parameters defined in `GetOrganizationMemberListDto`

#### Response

Returns a `ListResultDto<OrganizationMemberDto>` containing the list of members.

### Set Organization Member

Set a member in an organization.

```http
PUT /api/organizations/{organizationId}/members
```

#### Parameters

- `organizationId` (path parameter): Organization GUID

#### Request Body

```json
{
  // SetOrganizationMemberDto properties
}
```

### Set Member Role

Set a member's role in an organization.

```http
PUT /api/organizations/{organizationId}/member-roles
```

#### Parameters

- `organizationId` (path parameter): Organization GUID

#### Request Body

```json
{
  // SetOrganizationMemberRoleDto properties
}
```

### List Organization Permissions

Retrieve a list of organization permissions.

```http
GET /api/organizations/{organizationId}/permissions
```

#### Parameters

- `organizationId` (path parameter): Organization GUID

#### Response

Returns a `ListResultDto<PermissionGrantInfoDto>` containing the list of permissions.

## Status Codes

- 200: Successful operation
- 400: Invalid request
- 401: Unauthorized
- 403: Forbidden
- 404: Organization not found
- 500: Server error

## Error Handling

The API uses standard HTTP status codes for error responses. Detailed error messages are included in the response body when applicable.

## Required Permissions

Different endpoints require different permissions:
- View: `Organizations.Default`
- Edit: `Organizations.Edit`
- Delete: `Organizations.Delete`
- Manage Members: `Members.Manage`
- View Members: `Members.Default`