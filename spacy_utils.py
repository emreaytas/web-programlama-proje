import spacy
from spacy.matcher import PhraseMatcher

# Spacy modelini yükle
nlp = spacy.load("tr_core_news_lg")  # Türkçe model

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

def detect_category_product(sentence):
    """
    Cümlede kategori ve ürün tespiti yapar.
    
    Args:
        sentence (str): İşlenecek cümle
        
    Returns:
        tuple: (kategori, ürün, bulundu_mu)
    """
    sentence = sentence.lower()
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

    return found_category, found_product, bool(found_category or found_product) 