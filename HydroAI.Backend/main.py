"""
HydroAI Helper Agent – FastAPI backend for Blazor Helper_Agent.razor.
Loads .env from AI_Driven_Water_Supply.Presentation (single source of truth).
Run: uvicorn main:app --reload --host 127.0.0.1 --port 8000
"""
import os
import warnings

# Suppress deprecation warning until migrating to google.genai (must run before importing genai)
with warnings.catch_warnings():
    warnings.simplefilter("ignore", category=FutureWarning)
    import google.generativeai as genai
    from google.generativeai.types import HarmCategory, HarmBlockThreshold, CallableFunctionDeclaration, Tool
from google.ai.generativelanguage_v1beta.types.content import FunctionResponse as GlmFunctionResponse

import smtplib
import base64
import io
from PIL import Image
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
from email.mime.image import MIMEImage
from typing import Optional

from fastapi import FastAPI
from pydantic import BaseModel
from supabase import create_client, Client
from fastapi.middleware.cors import CORSMiddleware
from dotenv import load_dotenv

# --- 1. Load .env: prefer sibling Presentation folder (single source of truth) ---
_this_dir = os.path.dirname(os.path.abspath(__file__))
_env_in_presentation = os.path.join(_this_dir, "..", "AI_Driven_Water_Supply.Presentation", ".env")
load_dotenv()
if not os.getenv("GEMINI_API_KEY"):
    load_dotenv(_env_in_presentation)

SUPABASE_URL = os.getenv("SUPABASE_URL")
SUPABASE_KEY = os.getenv("SUPABASE_KEY") or os.getenv("SUPABASE_PUBLIC_KEY")
GEMINI_API_KEY = os.getenv("GEMINI_API_KEY")
EMAIL_SENDER = os.getenv("EMAIL_SENDER")
EMAIL_PASSWORD = os.getenv("EMAIL_PASSWORD")
ADMIN_EMAIL = os.getenv("ADMIN_EMAIL")

if not GEMINI_API_KEY:
    raise ValueError("GEMINI_API_KEY missing! Set it in AI_Driven_Water_Supply.Presentation/.env")

# --- 2. Setup Clients ---
genai.configure(api_key=GEMINI_API_KEY)
supabase: Client = create_client(SUPABASE_URL, SUPABASE_KEY)
app = FastAPI(title="HydroAI Helper Agent API")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

# --- Global stores ---
chat_sessions = {}
active_image_store = {}

# --- Native tools (Python functions; type hints required for SDK) ---

def find_water_providers(location_query: str) -> str:
    """
    Finds private water suppliers (Bottles, RO Plant, Tankers) based on location.
    Returns an HTML list of provider cards. Call when user asks for suppliers or delivery in an area.
    """
    print(f"🔍 Searching Suppliers: {location_query}")
    try:
        response = supabase.table("profiles").select("*").eq("role", "Provider").execute()
        data = response.data or []
        if not data:
            return "No providers found in this area."

        html = '<div class="row g-3">'
        for p in data:
            name = p.get("username", "Unknown")
            rating = p.get("rating", 0.0)
            img = p.get("profilepic", "")
            img_url = f"{SUPABASE_URL}/storage/v1/object/public/Avatar/{img}" if img else "/images/fallbackimg.jpg"
            html += f"""
            <div class="col-12" style="margin-bottom: 15px;">
                <div class="supplier-card">
                    <img src="{img_url}" style="width:50px;height:50px;border-radius:50%;object-fit:cover;">
                    <div class="card-body-custom">
                        <h5>{name} (⭐ {rating})</h5>
                        <a href="/supplier/{name}" class="select-btn">Select</a>
                    </div>
                </div>
            </div>"""
        html += "</div>"
        return html
    except Exception as e:
        return f"Error: {e}"


def report_issue_to_admin(issue_details: str) -> str:
    """
    Sends a complaint email to the Admin about a provider, delayed delivery, bad water quality, or app issues.
    Call when user wants to complain or report something.
    """
    print("📧 Sending Email...")
    try:
        msg = MIMEMultipart()
        msg["From"] = EMAIL_SENDER
        msg["To"] = ADMIN_EMAIL
        msg["Subject"] = "New Complaint (HydroAI)"

        session_id = "default_user"
        image_base64 = active_image_store.get(session_id)

        body = f"User Complaint:\n\n{issue_details}"
        if image_base64:
            body += "\n\n[Image Attached]"
        else:
            body += "\n(No image provided)"

        msg.attach(MIMEText(body, "plain"))

        if image_base64:
            try:
                image_data_str = image_base64.split(",")[1] if "," in image_base64 else image_base64
                img_data = base64.b64decode(image_data_str)
                msg.attach(MIMEImage(img_data, name="evidence.jpg"))
            except Exception as img_err:
                print(f"Image attach error: {img_err}")

        with smtplib.SMTP("smtp.gmail.com", 587) as server:
            server.starttls()
            server.login(EMAIL_SENDER, EMAIL_PASSWORD)
            server.sendmail(EMAIL_SENDER, ADMIN_EMAIL, msg.as_string())

        if session_id in active_image_store:
            del active_image_store[session_id]

        return "Don't worry, Boss! 🫡 Complaint forward kardi hai."
    except Exception as e:
        return f"Email Failed: {e}"


# --- Gemini tools: use CallableFunctionDeclaration so the SDK builds schema from our functions ---
_tool_declarations = [
    CallableFunctionDeclaration.from_function(find_water_providers),
    CallableFunctionDeclaration.from_function(report_issue_to_admin),
]
_tools = [Tool(function_declarations=_tool_declarations)]

# Map name -> callable (for executing function_call from model)
_tool_by_name = {d.name: d for d in _tool_declarations}

system_instruction = """You are 'HydroAI', an intelligent AI assistant for a private water delivery marketplace (NOT government pipeline water).
Your system connects consumers with private water suppliers who sell:
1. Water Bottles (19-liter cans)
2. RO Plant Water
3. Water Tankers (Bulk supply)

Rules:
1. If the user asks for water delivery, suppliers, or prices, mention Bottles, RO Plants, and Tankers. If they mention a location, ALWAYS call find_water_providers.
2. If the user mentions government pipelines, municipal water, or "line ka pani", politely clarify that this app is ONLY for private commercial water delivery (Bottles, Plants, Tankers).
3. If the user wants to complain about a provider, delayed delivery, bad water quality, or app issues, call report_issue_to_admin.
4. Keep your tone helpful, concise, and friendly. Use emojis like 💧, 🚚, 🚰."""

model = genai.GenerativeModel(
    model_name="gemini-flash-lite-latest",
    tools=_tools,
    system_instruction=system_instruction,
    safety_settings={
        HarmCategory.HARM_CATEGORY_HARASSMENT: HarmBlockThreshold.BLOCK_NONE,
        HarmCategory.HARM_CATEGORY_HATE_SPEECH: HarmBlockThreshold.BLOCK_NONE,
        HarmCategory.HARM_CATEGORY_SEXUALLY_EXPLICIT: HarmBlockThreshold.BLOCK_NONE,
        HarmCategory.HARM_CATEGORY_DANGEROUS_CONTENT: HarmBlockThreshold.BLOCK_NONE,
    },
)


class ChatRequest(BaseModel):
    message: Optional[str] = ""
    image: Optional[str] = None


@app.post("/chat")
async def chat_endpoint(request: ChatRequest):
    try:
        session_id = "default_user"

        # 1. Build user content (image + text)
        user_parts = []
        if request.image:
            active_image_store[session_id] = request.image
            try:
                img_str = request.image.split(",")[1] if "," in request.image else request.image
                image_bytes = base64.b64decode(img_str)
                image = Image.open(io.BytesIO(image_bytes))
                user_parts.append(image)
                user_parts.append(
                    "Look at this image. If it shows an issue with water delivery, broken bottle, or dirty water from a vendor, describe it and ask if I should report it to admin."
                )
            except Exception as e:
                print(f"Image Error: {e}")

        if request.message:
            user_parts.append(request.message)

        if not user_parts:
            return {"response": "Please send a message or an image."}

        # 2. Get or create chat session
        if session_id not in chat_sessions:
            chat_sessions[session_id] = model.start_chat(history=[])

        chat = chat_sessions[session_id]

        # 3. Send and handle function calls in a loop
        current_content = user_parts
        max_rounds = 10
        for _ in range(max_rounds):
            response = chat.send_message(current_content)

            if not response.candidates:
                return {"response": "No response from model."}

            text_parts = []
            function_calls = []

            for part in response.candidates[0].content.parts:
                if getattr(part, "text", None):
                    text_parts.append(part.text)
                if getattr(part, "function_call", None):
                    function_calls.append(part.function_call)

            if text_parts:
                return {"response": "\n".join(text_parts)}

            if not function_calls:
                return {"response": "I'm not sure how to respond. Try asking for 'suppliers' or 'report a complaint'."}

            # Execute each function call via CallableFunctionDeclaration and build follow-up
            follow_ups = []
            for fc in function_calls:
                name = getattr(fc, "name", None) or ""
                decl = _tool_by_name.get(name)
                if decl:
                    try:
                        fr = decl(fc)
                        follow_ups.append(fr)
                    except Exception as e:
                        follow_ups.append(GlmFunctionResponse(name=name, response={"result": str(e)}))
                else:
                    follow_ups.append(GlmFunctionResponse(name=name, response={"result": f"Unknown tool: {name}"}))
            current_content = follow_ups

    except Exception as e:
        print(f"Error: {str(e)}")
        if "default_user" in chat_sessions:
            del chat_sessions["default_user"]
        return {"response": f"System Error: {str(e)}"}


@app.get("/health")
def health():
    return {"status": "ok", "service": "HydroAI Helper Agent"}
