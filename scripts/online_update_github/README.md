# topdog_online_update

GitHub **pointer-only** repo. Hot-update payloads live on Hugging Face Bucket (not in this git repo).

| Purpose | URL |
|---------|-----|
| HF bucket (content + hot DLLs) | https://huggingface.co/buckets/liketocode789/topdog_online_update_data |
| Client resolve base | https://huggingface.co/buckets/liketocode789/topdog_online_update_data/resolve/ |
| Version pointer (optional mirror) | [version.json](version.json) |

## Protocol

1. `GET {resolve}/version.json`
2. `GET {resolve}/manifest.json`
3. Download `content/**` and `hotupdate/**` listed in the manifest (sha256 check)

Version format: `YYYYMM.D.N`. Online play requires matching local versions.

## Publish

```powershell
cd e:\game_dev\top_dog_unity
# Default: HF only (does not touch GitHub)
.\scripts\publish_online_update.ps1 -Push

# Rare: also refresh this GitHub pointer repo
.\scripts\publish_online_update.ps1 -Push -PushGitHub
```
