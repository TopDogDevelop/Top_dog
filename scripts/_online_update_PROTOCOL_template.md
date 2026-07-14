# TopDog Online Update Protocol

- Resource store: Hugging Face Bucket https://huggingface.co/buckets/liketocode789/topdog_online_update_data
- Client base URL: https://huggingface.co/buckets/liketocode789/topdog_online_update_data/resolve/
- Version format: `YYYYMM.D.N` (example `202607.14.1`; no letters; see docs/VERSION.md)
- Flow: `GET version.json` -> `manifest.json` -> download `content/**`
- LAN: identical local `contentVersion` strings required
- GitHub repo https://github.com/TopDogDevelop/topdog_online_update is a human pointer only (no content)
