using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;


namespace WasteCollectionSystem.Controllers
{
    [ApiController]
    [Route("api/chatbot")]
    public class ChatbotController : ControllerBase
    {


        [HttpGet("ask")]
        public IActionResult Ask(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Query cannot be empty.");
            }

            string content = GetResponse(query);
            return Content(content, "text/plain");
        }

        private string GetResponse(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return "Please ask me a question about waste collection, payments, tracking, or support!";
            }

            string lowerQuery = query.ToLowerInvariant().Trim();
            var keywords = lowerQuery.Split(new[] { ' ', '?', '!', '.', ',', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

            // Greetings - Check first
            var greetingWords = new[] { "hi", "hello", "hey", "greetings", "good morning", "good afternoon", "good evening", "morning", "afternoon", "evening", "greet" };
            if (greetingWords.Any(g => lowerQuery.Contains(g)) || keywords.Intersect(greetingWords).Any())
            {
                return "Hello! I'm WasteBot, your AI assistant for waste collection services. I can help you with:\n\n" +
                       "- Requesting waste collection\n" +
                       "- Payment information (MTN MoMo)\n" +
                       "- Tracking your truck\n" +
                       "- Contacting support\n\n" +
                       "What would you like to know?";
            }

            // Support / Contact / Help - High priority
            var supportKeywords = new[] { "support", "contact", "help", "call", "email", "phone", "number", "telephone", "issue", "problem", "complaint", "assistance", "customer service", "reach", "get in touch" };
            if (supportKeywords.Any(k => lowerQuery.Contains(k)) || 
                lowerQuery.Contains("+250") || 
                lowerQuery.Contains("787942500") ||
                lowerQuery.Contains("how can i contact") ||
                lowerQuery.Contains("where can i call"))
            {
                return "SUPPORT CONTACT INFORMATION\n\n" +
                       "Our support team is available 24/7 to assist you!\n\n" +
                       "Phone: +250787942500\n" +
                       "Email: support@greentrack.rw\n\n" +
                       "Feel free to call or email us anytime for assistance with:\n" +
                       "- Waste collection requests\n" +
                       "- Payment issues\n" +
                       "- Tracking problems\n" +
                       "- General inquiries\n\n" +
                       "We're here to help!";
            }

            // Request Waste Collection - Comprehensive matching
            var wasteKeywords = new[] { "request", "waste", "collection", "pickup", "pick up", "rubbish", "trash", "garbage", "disposal", "schedule", "book", "order" };
            var actionWords = new[] { "how", "want", "need", "can i", "should i", "do i", "to request", "to schedule", "to book" };
            
            if (wasteKeywords.Any(k => lowerQuery.Contains(k)) && 
                (actionWords.Any(a => lowerQuery.Contains(a)) || 
                 lowerQuery.Contains("request") || 
                 lowerQuery.Contains("schedule") ||
                 lowerQuery.Contains("book")))
            {
                return "HOW TO REQUEST WASTE COLLECTION\n\n" +
                       "1. Log in to your account (or create one if you're new)\n" +
                       "2. Go to 'Request Waste Collection' from your dashboard\n" +
                       "3. Fill in the details:\n" +
                       "   - Select waste type (Plastic, Organic, Paper, Metal, E-waste, or Mixed)\n" +
                       "   - Choose your location (use the map to pinpoint exact location)\n" +
                       "   - Set preferred date & time (optional)\n" +
                       "   - Add any notes or upload a photo (optional)\n" +
                       "4. Submit your request\n\n" +
                       "Once submitted, your request will be reviewed and assigned to a collection truck. You'll receive notifications about the status!\n\n" +
                       "Need help? Contact support at +250787942500";
            }

            // Payment / MoMo - Enhanced
            var paymentKeywords = new[] { "pay", "payment", "momo", "mtn", "money", "payment method", "how to pay", "payment option", "mobile money", "pay with", "payment process" };
            if (paymentKeywords.Any(k => lowerQuery.Contains(k)) && 
                !lowerQuery.Contains("how much") && 
                !lowerQuery.Contains("what is the cost") &&
                !lowerQuery.Contains("price") &&
                !lowerQuery.Contains("cost"))
            {
                return "PAYMENT INFORMATION\n\n" +
                       "Yes! We accept payments through MTN Mobile Money (MoMo) for secure and convenient transactions.\n\n" +
                       "Payment Process:\n" +
                       "1. After submitting your waste collection request, you'll see the payment option\n" +
                       "2. Select items from your cart\n" +
                       "3. Proceed to payment\n" +
                       "4. Complete payment via MTN MoMo\n\n" +
                       "Benefits:\n" +
                       "- Secure transactions\n" +
                       "- Quick and easy\n" +
                       "- Instant confirmation\n\n" +
                       "All payments are processed securely through MTN MoMo Sandbox. For payment issues, contact support at +250787942500";
            }

            // Tracking - Enhanced
            var trackingKeywords = new[] { "track", "tracking", "gps", "location", "truck", "driver", "where is", "status", "live", "real-time", "monitor", "follow" };
            if (trackingKeywords.Any(k => lowerQuery.Contains(k)) || 
                lowerQuery.Contains("where is my") ||
                lowerQuery.Contains("how to track"))
            {
                return "TRUCK TRACKING\n\n" +
                       "You can track your assigned waste collection truck in real-time using GPS live tracking!\n\n" +
                       "How to Track:\n" +
                       "1. Go to your Dashboard\n" +
                       "2. View your active requests\n" +
                       "3. Click on a request to see the truck location on the map\n" +
                       "4. See real-time updates as the truck approaches\n\n" +
                       "Features:\n" +
                       "- Live GPS tracking\n" +
                       "- Estimated arrival time\n" +
                       "- Driver information\n" +
                       "- Request status updates\n\n" +
                       "If you have trouble tracking, contact support at +250787942500";
            }

            // Status / Check Request
            if (lowerQuery.Contains("status") || 
                lowerQuery.Contains("check") || 
                lowerQuery.Contains("my request") ||
                lowerQuery.Contains("where is my request"))
            {
                return "CHECK REQUEST STATUS\n\n" +
                       "To check your waste collection request status:\n\n" +
                       "1. Go to your Dashboard\n" +
                       "2. Navigate to 'My Requests' or 'History'\n" +
                       "3. View all your requests with their current status:\n" +
                       "   - Pending: Awaiting assignment\n" +
                       "   - Assigned: Truck assigned, tracking available\n" +
                       "   - In Progress: Collection in progress\n" +
                       "   - Completed: Successfully collected\n\n" +
                       "You'll also receive notifications when your request status changes!\n\n" +
                       "Need help? Call +250787942500";
            }

            // Account / Login / Register
            if (lowerQuery.Contains("account") || 
                lowerQuery.Contains("login") || 
                lowerQuery.Contains("register") ||
                lowerQuery.Contains("sign up") ||
                lowerQuery.Contains("create account"))
            {
                return "ACCOUNT INFORMATION\n\n" +
                       "To create an account:\n" +
                       "1. Click on 'Register' in the top navigation\n" +
                       "2. Fill in your details (name, email, phone)\n" +
                       "3. Set a secure password\n" +
                       "4. Verify your email\n\n" +
                       "To log in:\n" +
                       "1. Click 'Login' in the top navigation\n" +
                       "2. Enter your email and password\n" +
                       "3. Access your dashboard\n\n" +
                       "Guest users can also submit requests without creating an account!\n\n" +
                       "For account issues, contact support at +250787942500";
            }

            // Waste Types
            if (lowerQuery.Contains("waste type") || 
                lowerQuery.Contains("what types") ||
                lowerQuery.Contains("kinds of waste"))
            {
                return "ACCEPTED WASTE TYPES\n\n" +
                       "We collect the following types of waste:\n\n" +
                       "- Plastic: Bottles, containers, packaging\n" +
                       "- Organic: Food waste, garden waste\n" +
                       "- Paper: Newspapers, cardboard, documents\n" +
                       "- Metal: Cans, scrap metal\n" +
                       "- E-waste: Electronic devices, batteries\n" +
                       "- Mixed: Combination of different types\n\n" +
                       "When requesting collection, simply select the appropriate waste type from the dropdown menu.\n\n" +
                       "Questions? Call +250787942500";
            }

            // Pricing / Cost
            if ((lowerQuery.Contains("price") || 
                lowerQuery.Contains("cost") ||
                lowerQuery.Contains("how much") ||
                lowerQuery.Contains("fee") ||
                lowerQuery.Contains("charge")) &&
                !lowerQuery.Contains("payment method") &&
                !lowerQuery.Contains("how to pay"))
            {
                return "PRICING INFORMATION\n\n" +
                       "Pricing for waste collection varies based on:\n" +
                       "- Waste type\n" +
                       "- Quantity/amount\n" +
                       "- Location\n\n" +
                       "To see pricing:\n" +
                       "1. Submit a waste collection request\n" +
                       "2. The system will calculate the amount\n" +
                       "3. View the price before confirming payment\n\n" +
                       "All prices are transparent and shown before you complete your request.\n\n" +
                       "For specific pricing inquiries, please contact support at +250787942500";
            }

            // Thank you / Appreciation
            if (lowerQuery.Contains("thank") || 
                lowerQuery.Contains("thanks") ||
                lowerQuery.Contains("appreciate"))
            {
                return "You're very welcome!\n\n" +
                       "I'm here to help anytime you need assistance with:\n" +
                       "- Waste collection requests\n" +
                       "- Payment questions\n" +
                       "- Tracking your truck\n" +
                       "- General support\n\n" +
                       "Have a great day! If you need anything else, just ask or call +250787942500";
            }

            // Fallback with helpful suggestions
            return "I'm not sure I understood that question.\n\n" +
                   "I can help you with:\n\n" +
                   "- Waste Collection: 'How do I request waste collection?'\n" +
                   "- Payments: 'How do I pay?' or 'Can I pay with MTN MoMo?'\n" +
                   "- Tracking: 'How do I track the truck?'\n" +
                   "- Support: 'How can I contact support?' or 'What's your phone number?'\n" +
                   "- Status: 'How do I check my request status?'\n\n" +
                   "Try asking one of these questions, or contact our support team directly at +250787942500 for immediate assistance!";
        }
    }
}
