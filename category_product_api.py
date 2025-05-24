from fastapi import FastAPI, Request
from pydantic import BaseModel
from typing import List, Optional
import uvicorn
import re
import spacy
from spacy.matcher import PhraseMatcher

app = FastAPI(title="Kategori & Ürün NLP API")

# Spacy modelini yükle
nlp = spacy.load("tr_core_news_sm")  # Türkçe model

# Örnek kategori ve ürün listesi
data = {
    "telefon": ["iphone 14", "samsung galaxy s23", "xiaomi redmi note 12"],
    "bilgisayar": ["macbook air", "dell xps 13", "lenovo thinkpad"],
    "buzdolabı": ["arçelik nofrost", "vestel retro", "siemens iq500"]
}

# Tüm ürünleri tek listede de tut
all_products = [urun for urunler in data.values() for urun in urunler]
all_categories = list(data.keys())

# PhraseMatcher oluştur
matcher = PhraseMatcher(nlp.vocab)
# Ürünleri PhraseMatcher'a ekle
for product in all_products:
    matcher.add("PRODUCT", [nlp(product)])

class DetectRequest(BaseModel):
    sentence: str

class DetectResponse(BaseModel):
    category: Optional[str]
    product: Optional[str]
    found: bool

@app.get("/health")
def health():
    return {"status": "up", "message": "Kategori & Ürün NLP servisi çalışıyor"}

@app.post("/detect-category-product", response_model=DetectResponse)
def detect_category_product(req: DetectRequest):
    sentence = req.sentence.lower()
    doc = nlp(sentence)
    found_category = None
    found_product = None

    # PhraseMatcher ile ürünleri ara
    matches = matcher(doc)
    for match_id, start, end in matches:
        span = doc[start:end]
        found_product = span.text
        # Ürünün kategorisini bul
        for cat, plist in data.items():
            if found_product in plist:
                found_category = cat
                break
        break

    # Eğer ürün bulunamadıysa kategori ara
    if not found_category:
        for cat in all_categories:
            if cat in sentence:
                found_category = cat
                break

    return {
        "category": found_category,
        "product": found_product,
        "found": bool(found_category or found_product)
    }

# Gerekirse ayrı endpointler de eklenebilir
# @app.post("/detect-category")
# @app.post("/detect-product")

if __name__ == "__main__":
    uvicorn.run("category_product_api:app", host="0.0.0.0", port=8000, reload=True) 