# Environment Setup for Test Scripts

## Overview

The test scripts now use environment variables for sensitive authentication credentials. This approach keeps credentials out of the codebase and prevents accidental commits of sensitive information.

## Setup Instructions

### 1. Copy the Template

```bash
cp set-env.template.sh set-env.sh
```

### 2. Edit Credentials

Edit `set-env.sh` and replace the placeholder values with your actual credentials:

```bash
export AEVATAR_USERNAME="your-actual-username"
export AEVATAR_PASSWORD="your-actual-password"
export AEVATAR_CLIENT_ID="your-actual-client-id"
export AEVATAR_SCOPE="your-actual-scope"
```

### 3. Load Environment Variables

```bash
source ./set-env.sh
```

### 4. Run Test Scripts

```bash
./test-tool-calling.sh
```

## Important Security Notes

1. **NEVER commit `set-env.sh` to version control** - It's already added to `.gitignore`
2. **Keep credentials secure** - Don't share your `set-env.sh` file
3. **Use different credentials for different environments** - Don't use production credentials for testing

## Environment Variables Reference

| Variable | Description | Example |
|----------|-------------|---------|
| `AEVATAR_USERNAME` | Authentication username | `admin` |
| `AEVATAR_PASSWORD` | Authentication password | `********` |
| `AEVATAR_CLIENT_ID` | OAuth client ID | `AevatarAuthServer` |
| `AEVATAR_SCOPE` | OAuth scope | `Aevatar` |

## Troubleshooting

If you get an error about missing environment variables:

1. Make sure you've sourced the script: `source ./set-env.sh` (not just `./set-env.sh`)
2. Check that all required variables are set: `env | grep AEVATAR`
3. Verify your credentials are correct

## Alternative: Direct Export

You can also set environment variables directly:

```bash
export AEVATAR_USERNAME="admin"
export AEVATAR_PASSWORD="your-password"
export AEVATAR_CLIENT_ID="AevatarAuthServer"
export AEVATAR_SCOPE="Aevatar"
```

Or create a `.env` file and use a tool like `direnv` for automatic loading. 