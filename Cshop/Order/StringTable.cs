
using System;
using Claw.Modules.Eshop.Models;

namespace Claw.Modules.Eshop { internal class StringTable : Order.StringTable { } }

namespace Claw.Modules.Eshop.Order
{
    public class StringTable : Product.StringTable
    {
        public static string DropShip
        {
            get { return "Dropship".ModLocalize(); }
        }
        public static string RemoveDropShip
        {
            get { return "Remove dropship".ModLocalize(); }
        }
        public static string AddDropShip
        {
            get { return "Add dropship".ModLocalize(); }
        }

        public static string RecaptureRecur
        {
            get { return "Recapture Recur".ModLocalize("Re-capture"); }
        }

        public static string ConfirmRecaptureRecur
        {
            get { return "confirm_Recapture_Recur".ModLocalize("Are you sure you want to re-capture this order?<br>You cannot undo this action"); }
        }

        public static string ConfirmPackage
        {
            get { return "Confirm Package".ModLocalize("Bekreft pakken"); }
        }
        public static string Confirm
        {
            get { return "Confirm".ModLocalize(); }
        }

        public static string PaymentReleaseRemainingAmountError
        {
            get { return "Payment Release Remaining Amount Errror".ModLocalize("Release remaining amount failed!"); }
        }

        internal static string Giftcard
        {
            get { return "Giftcard".ModLocalize(); }
        }

        public static string UpdateCompanyOwner
        {
            get { return "Update company owner".ModLocalize(); }
        }

        public static string ManageCompanies
        {
            get { return "Manage companies".ModLocalize(); }
        }

        public static string Companies
        {
            get { return "Companies".ModLocalize(); }
        }

        public static string ResetPassword
        {
            get { return "Reset password".ModLocalize(); }
        }

        public static string PasswordMismatch
        {
            get { return "Password mismatch".ModLocalize(); }
        }

        public static string CustomerDiscountInfo
        {
            get { return "Customer Discount Info".ModLocalize("Discount info"); }
        }

        public static string CustomerDiscountSelectCate
        {
            get { return "Customer Discount Select Cate".ModLocalize("Select Categories"); }
        }

        public static string CustGroupDiscountSelectCate
        {
            get { return "Custgroup Discount Select Cate".ModLocalize("Select Categories"); }
        }

        public static string ManualShippingBooking
        {
            get { return "Manual shipping booking".ModLocalize(); }
        }

        public static string ManageShippingBookings
        {
            get { return "Manage manual shipping bookings".ModLocalize(); }
        }

        public static string AddEditShippingBooking(int id)
        {
            if (id == 0) return "Add manual shipping booking".ModLocalize();
            else return "Edit manual shipping booking".ModLocalize();
        }

        public static string ManageRecurOrders
        {
            get { return "Manage recurring orders".ModLocalize(); }
        }

        public static string AddEditRecurOrder(int id)
        {
            if (id == 0) return "Add recurring order".ModLocalize();
            else return "Edit recurring order".ModLocalize();
        }

        public static string RecurOrders
        {
            get { return "Recurring orders".ModLocalize(); }
        }

        public static string RecurOrder
        {
            get { return "Recurring order".ModLocalize(); }
        }

        public static string RecurOrderLogTemplSucceeded
        {
            get { return "Recurring order log templ succeeded".ModLocalize("Transaksjonen Klarna RecurringOrder ble godkjent ved {0} med transaksjon# {1}"); }
        }

        public static string RecurOrderLogTemplFailed
        {
            get { return "Recurring order log templ failed".ModLocalize("Transaksjonen Klarna RecurringOrder ble mislyktes ved {0} med feilmelding: {1}"); }
        }

        public static string SendEmailFailedTempl
        {
            get { return "Send order email failed templ".ModLocalize("Send email for order {0} failed with error message: {1}"); }
        }

        public static string RecurEach(Models.RecurringType type)
        {
            switch (type)
            {
                case RecurringType.Daily:
                    return "Day".ModLocalize();
                case RecurringType.Weekly:
                    return "Week".ModLocalize();
                case RecurringType.Monthly:
                    return "Month".ModLocalize();
                case RecurringType.Bimonthly:
                    return "Bimonth".ModLocalize();
                case RecurringType.Trimonthly:
                    return "Trimonth".ModLocalize();
                case RecurringType.Quadmonthly:
                    return "Quadmonth".ModLocalize();
                case RecurringType.Semiyearly:
                    return "Semiyear".ModLocalize();
                case RecurringType.Yearly:
                    return "Year".ModLocalize();
                default:
                    throw new InvalidOperationException("StringTable.RecurEach");
            }
        }

        public static string RecurInfoDesc(RecurringType type, bool discount, int recurEach = 1)
        {
            var name = "Recurring info desc " + type + " each " + recurEach + (discount ? " discount" : "");
            switch (type)
            {
                case RecurringType.Daily:
                    return name.ModLocalize("{0} (Every " + recurEach + " days" + (discount ? ". Discount: {2}%" : "") + ")");
                case RecurringType.Weekly:
                    return name.ModLocalize("{0} (Every " + recurEach + " weeks" + (discount ? ". Discount: {2}%" : "") + ")");
                case RecurringType.Monthly:
                    return name.ModLocalize("{0} (Every " + recurEach + " months" + (discount ? ". Discount: {2}%" : "") + ")");
                case RecurringType.Bimonthly:
                    return name.ModLocalize("{0} (Every " + recurEach + " bimonths" + (discount ? ". Discount: {2}%" : "") + ")");
                case RecurringType.Trimonthly:
                    return name.ModLocalize("{0} (Every " + recurEach + " trimonths" + (discount ? ". Discount: {2}%" : "") + ")");
                case RecurringType.Quadmonthly:
                    return name.ModLocalize("{0} (Every " + recurEach + " quadmonths" + (discount ? ". Discount: {2}%" : "") + ")");
                case RecurringType.Semiyearly:
                    return name.ModLocalize("{0} (Every " + recurEach + " semiyears" + (discount ? ". Discount: {2}%" : "") + ")");
                case RecurringType.Yearly:
                    return name.ModLocalize("{0} (Every " + recurEach + " years" + (discount ? ". Discount: {2}%" : "") + ")");
                default:
                    throw new InvalidOperationException("StringTable.RecurTimesDesc");
            }
        }

        public static string RecurOrderResult(bool ok)
        {
            return ok
                ? "Recurring order succeeded".ModLocalize("Succeeded")
                : "Recurring order failed".ModLocalize("Failed");
        }

        public static string RecuringEach
        {
            get { return "Recurring each".ModLocalize("Each"); }
        }


        public static string ManageOffers
        {
            get { return "Manage offers".ModLocalize(); }
        }

        public static string AddEditOffer(int id)
        {
            if (id == 0) return "Add offer".ModLocalize();
            else return "Edit offer".ModLocalize();
        }

        public static string Offers
        {
            get { return "Offers".ModLocalize(); }
        }

        public static string Offer
        {
            get { return "Offer".ModLocalize(); }
        }

        public static string ManageOrders
        {
            get { return "Manage orders".ModLocalize("Manage order"); }
        }

        public static string AddEditOrder(int id)
        {
            if (id == 0) return "Add order".ModLocalize();
            else return "Edit order".ModLocalize();
        }

        public static string Orders
        {
            get { return "Orders".ModLocalize(); }
        }

        public static string Order
        {
            get { return "Order".ModLocalize(); }
        }

        public static string SentEmail
        {
            get { return "Sent email".ModLocalize(); }
        }

        public static string SentSMS
        {
            get { return "Sent SMS".ModLocalize(); }
        }

        public static string SendOrder
        {
            get { return "Send order".ModLocalize("Send"); }
        }

        public static string ShippingBookingBundle
        {
            get { return "Shipping Booking Bundle".ModLocalize("Shipping booking"); }
        }

        public static string SendOrdersResult(bool succeeded)
        {
            return succeeded
                ? "Send orders succeeded".ModLocalize("Send {0} orders successfully")
                : "Send orders failed".ModLocalize("Send failed, succeeded {1}/{0} orders");
        }

        public static string ForceSendOrder
        {
            get { return "Force Send Order".ModLocalize("Force"); }
        }

        public static string HentOrder
        {
            get { return "Hent order".ModLocalize("Hent"); }
        }
        public static string CancelOrder
        {
            get { return "Cancel order".ModLocalize("Cancel"); }
        }

        public static string Invoice
        {
            get { return "Invoice".ModLocalize(); }
        }

        public static string InvoiceNo
        {
            get { return "Invoice no".ModLocalize(); }
        }

        public static string OrderNo
        {
            get { return "Order no".ModLocalize(); }
        }

        public static string Res
        {
            get { return "Res".ModLocalize(); }
        }

        public static string CouponContextError
        {
            get { return "coupon_context_error".ModLocalize("Can not use the coupon code in this context"); }
        }

        public static string SentOn
        {
            get { return "Sent on".ModLocalize(); }
        }

        public static string Transaction
        {
            get { return "Transaction".ModLocalize(); }
        }

        public static string Track
        {
            get { return "Track".ModLocalize(); }
        }

        public static string CustomerInfoChanged
        {
            get { return "customer_info_change".ModLocalize("Customer information changed"); }
        }

        public static string AccountInformation
        {
            get { return "Account information".ModLocalize(); }
        }

        public static string BillingAddress
        {
            get { return "Billing address".ModLocalize(); }
        }

        public static string BillingInfoChanged
        {
            get { return "billing_info_change".ModLocalize("Billing address changed"); }
        }

        public static string ShippingAddress
        {
            get { return "Shipping address".ModLocalize(); }
        }

        public static string ShippingInfoChanged
        {
            get { return "shipping_info_changed".ModLocalize("Delivery address changed"); }
        }

        public static string PaymentInformation
        {
            get { return "Payment information".ModLocalize(); }
        }

        public static string ShippingInformation
        {
            get { return "Shipping information".ModLocalize("Shipping & Handing Information"); }
        }

        public static string DeliveryDateInformation
        {
            get { return "Delivery date information".ModLocalize("Delivery Date Information"); }
        }

        public static string ItemsOrder
        {
            get { return "Items ordered".ModLocalize(); }
        }

        public static string CommentsHistory
        {
            get { return "Comments history".ModLocalize(); }
        }

        public static string AddOrderComments
        {
            get { return "Add order comments".ModLocalize(); }
        }

        public static string DeleteComment
        {
            get { return "Delete comment".ModLocalize(); }
        }

        public static string SubmitComment
        {
            get { return "Submit comment".ModLocalize(); }
        }

        public static string NotifyCustomerEmail
        {
            get { return "Notify customer email".ModLocalize("Notify customer by email"); }
        }

        public static string TotalVat
        {
            get { return "Total vat".ModLocalize(); }
        }

        public static string OrderTotals
        {
            get { return "Order totals".ModLocalize(); }
        }

        public static string ErrOrderDetailQuantity
        {
            get { return "errmsg_orderdetail_quantity".ModLocalize("The value must not be greater than {0}"); }
        }

        public static string ShippingTracking
        {
            get { return "Tracking".ModLocalize("Tracking"); }
        }

        public static string SentEmailToCustomer
        {
            get { return "Sent email to customer".ModLocalize("Send email to customer"); }
        }

        public static string SentSMSToCustomer
        {
            get { return "Sent SMS to customer".ModLocalize(); }
        }

        public static string UpdateTrackingOrder
        {
            get { return "Update tracking order".ModLocalize("Update tracking order"); }
        }
        public static string SentReturnForm
        {
            get { return "Sent return form".ModLocalize("Sent return form"); }
        }

        public static string UpdateInvoice
        {
            get { return "Update invoice".ModLocalize("Update invoice"); }
        }

        public static string UpdateOrderTrackingId
        {
            get { return "Update order tracking id".ModLocalize(); }
        }

        public static string UpdateOrderDetailTags
        {
            get { return "Update order detail tags".ModLocalize(); }
        }

        public static string UpdateOrderStatus
        {
            get { return "Update order status".ModLocalize("Update order status"); }
        }

        public static string OrderStatusChangeLogTemplate
        {
            get { return "order_status_change_log_template".ModLocalize("Order status was changed to {0}"); }
        }

        public static string OrderStatus
        {
            get { return "Order status".ModLocalize("Order status"); }
        }

        public static string OrderPrev
        {
            get { return "Order prev".ModLocalize("Previous"); }
        }

        public static string OrderNext
        {
            get { return "Order next".ModLocalize("Next"); }
        }

        public static string PrintInvoice
        {
            get { return "Print invoice".ModLocalize("Print invoice"); }
        }
        //public static string PrintDelivery
        //{
        //    get { return "print delivery".ModLocalize("print delivery"); }
        //}
        public static string RegisterRestorder
        {
            get { return "Register restorder".ModLocalize("Register backorder"); }
        }

        public static string OrderCaptureTransactionMsg
        {
            get { return "Order_Capture_Transaction_Msg".ModLocalize("Capturing transaction..."); }
        }

        public static string OrderCheckTransactionMsg
        {
            get { return "Order_Check_Transaction_Msg".ModLocalize("Checking transaction..."); }
        }

        public static string OrderChangeStatusMsg
        {
            get { return "Order_Change_Status_Msg".ModLocalize("Changing status..."); }
        }

        public static string OrderFilterByCustomerHint
        {
            get { return "order_filter_by_customer_hint".ModLocalize("Orders by customer"); }
        }

        public static string ErrMsgOrderCaptureFailed
        {
            get { return "ErrMsg_Order_Capture_Failed".ModLocalize("Error during process transaction. Error message: {0}. Please capture this transaction manually"); }
        }

        public static string ErrMsgOrderCaptureFailedWEmailNoti
        {
            get { return "ErrMsg_Order_Capture_Failed_with_email_notify".ModLocalize("There is error when proccessing the transaction on Nets system. The error message is: {0}. This order and subscription order will be cancelled"); }
        }

        public static string ErrMsgOrderCaptureFailedWEmailNotiRecurOneTime
        {
            get { return "ErrMsg_Order_Capture_Failed_with_email_notify_recuronetime".ModLocalize("There is error when proccessing the transaction on Nets system. The error message is: {0}."); }
        }

        public static string ErrMsgOrderCaptureFailedNoAttempt
        {
            get { return "ErrMsg_Order_Capture_Failed_NoAttempt".ModLocalize("Error during process transaction. Error message: {0}. The order will be canceled"); }
        }

        public static string OrderCaptureFailedLogTempl
        {
            get { return "Order_Capture_Failed_Log_Templ".ModLocalize("Error during process transaction. Error message: {0}. Please capture this transaction manually"); }
        }

        public static string OrderCaptureOkLogTempl
        {
            get { return "Order_Capture_Ok_Log_Templ".ModLocalize("Captured success with transaction {0} on system {1}"); }
        }

        public static string OrderCaptureLogKlarna
        {
            get { return "Order_Capture_Log_Klarna".ModLocalize("The reservation number {0} was activated by klarna at {1} with status:{2}, OCR#: {3}"); }
        }

        public static string OrderCancelLogTempl
        {
            get { return "Order_Cancel_Log_Templ".ModLocalize("Cancelled transaction {0} on system {1}. Cancelled order#: {2} by {3} at {4}"); }
        }

        public static string OrderReturnPaymentLogTempl
        {
            get { return "Order_return_payment_Log_Templ".ModLocalize("Return transaction {0} on system {1} for order# {2}"); }
        }

        public static string OrderPaymentCreditFailedLog
        {
            get { return "Order_Payment_Credit_Log_Templ".ModLocalize("{0} Credit failed with message: {1}"); }
        }

        public static string OrderPaymentCreditSkipRefundLog
        {
            get { return "Order_Payment_Credit_Skip_Refund_Log_Templ".ModLocalize("{0} Credit skipped refunding by user"); }
        }

        public static string OrderPaymentCreditFailedOriginLog
        {
            get { return "Order_Payment_Credit_Origin_Log_Templ".ModLocalize("{0} Credit for order {1} failed with message: {2}"); }
        }

        public static string OrderPaymentCreditSkipRefundOriginLog
        {
            get { return "Order_Payment_Credit_Skip_Refund_Origin_Log_Templ".ModLocalize("{0} Credit for order {1} skipped refunding by user"); }
        }

        public static string OrderCreditLogTempl
        {
            get { return "Order_Credit_Log_Templ".ModLocalize("Returned from order {0}, Invoice {1} by {2}"); }
        }

        public static string OrderCreditOriginLogTempl
        {
            get { return "Order_CreditOrigin_Log_Templ".ModLocalize("Returned with new order {0} by {1}"); }
        }

        public static string OrderDetailChanged
        {
            get { return "Orderdetail changed".ModLocalize("Order details changed"); }
        }

        public static string OrderDetailSaveConfirmMsg
        {
            get { return "Orderdetail save confirm msg".ModLocalize("You have added new items to this order. Are you sure you want to continue? <br>This action cannot be undone!"); }
        }

        public static string OrderSendConfirmMsg
        {
            get { return "Order send confirm msg".ModLocalize("Are you sure you want to send this order?<br> You cannot undo this action!"); }
        }

        public static string OrderHentConfirmMsg
        {
            get { return "Order hent confirm msg".ModLocalize("Are you sure you want to hent this order?<br> You cannot undo this action!"); }
        }

        public static string OrderCreateRestConfirmMsg
        {
            get { return "Order create rest confirm msg".ModLocalize("Are you sure you want to register new backorder with selected products? <br> You cannot undo this action!"); }
        }

        public static string OrderCreditConfirmMsg
        {
            get { return "Order credit confirm msg".ModLocalize("Are you sure you want to return selected products? <br> You cannot undo this action!"); }
        }

        public static string EditOrderDetails
        {
            get { return "edit orderdetails".ModLocalize("Edit order details"); }
        }

        public static string OrderDetailAddItemLogTempl
        {
            get { return "Orderdetail add item log templ".ModLocalize("Added new item(s) {0} to order by {1}. Reason: {2}"); }
        }

        public static string OrderDetailEditItemLogTempl
        {
            get { return "Orderdetail edit item log templ".ModLocalize("Updated '{0}' of '{1}', former value: '{2}' by {3}. Reason: {4}"); }
        }

        public static string AddEditCustomer(int id)
        {
            if (id == 0) return "Add customer".ModLocalize();
            else return "Edit customer".ModLocalize();
        }

        public static string ManageCustomers
        {
            get { return "Manage customers".ModLocalize("Manage customers"); }
        }

        public static string CustGroup
        {
            get { return "CustGroup".ModLocalize("Group"); }
        }

        public static string ManageCustomerGroups
        {
            get { return "Manage customer groups".ModLocalize("Manage customer groups"); }
        }

        public static string ManageGroupDiscounts
        {
            get { return "Manage group discounts".ModLocalize(); }
        }

        public static string GroupDiscounts
        {
            get { return "Group discounts".ModLocalize(); }
        }

        public static string GroupDiscount
        {
            get { return "Group discount".ModLocalize(); }
        }

        public static string AddEditGroupDiscount(int id)
        {
            if (id == 0) return "Add group discount".ModLocalize();
            else return "Edit group discount".ModLocalize();
        }

        public static string ConfirmCancelOrder
        {
            get { return "confirm_cancel_order".ModLocalize("Are you sure you want to cancel this order?<br>You cannot undo this action"); }
        }

        public static string ConfirmCancelOrderOnError
        {
            get { return "confirm_cancel_order_on_error".ModLocalize("There is error during cancel transaction on {0}. Error message: {1}. <br>Are you sure you want to continue this process and process transaction manually?"); }
        }

        public static string ErrMsgCancelOrderHavingBackOrders
        {
            get { return "ErrMsg_Cancel_Order_having_BackOrders".ModLocalize("The order has at least one backorder that cannot be cancelled!"); }
        }

        public static string ErrMsgCancelOrderHavingRestOrders
        {
            get { return "ErrMsg_Cancel_Order_having_RestOrders".ModLocalize("The order has at least one backorder that in status [RestOrder]!"); }
        }

        public static string SendMailToCustomerTitle
        {
            get { return "Send mail to customer".ModLocalize(); }
        }

        public static string SendSMSToCustomerTitle
        {
            get { return "Send SMS to customer".ModLocalize(); }
        }

        public static string SubTotalExclVAT
        {
            get { return "Sub total excl vat".ModLocalize("Sub-total(excl.VAT)"); }
        }

        public static string SubTotalInlVAT
        {
            get { return "Sub total inl vat".ModLocalize("Sub-total(inl.VAT)"); }
        }
        public static string PaymentExclVat
        {
            get { return "Payment excl_vat".ModLocalize("Payment(excl.VAT)"); }
        }

        public static string TotalDiscountRecurrOrder
        {
            get { return "TotalDiscountRecurrOrder".ModLocalize("Discount value:"); }
        }

        public static string ShippingHangdingExclVAT
        {
            get { return "Shipping hangding excl vat".ModLocalize("Shipping & Hangding(excl.VAT)"); }
        }

        public static string SubTotalExclVATAfterDiscount
        {
            get { return "Total excluding VAT after discount".ModLocalize("Total (excluding VAT) after discount"); }
        }

        public static string PrintEDI
        {
            get { return "Print EDI".ModLocalize("Print EDI"); }
        }

        public static string PrintProteria
        {
            get { return "Print proteria".ModLocalize("Print proteria"); }
        }

        public static string PrintPackingslip
        {
            get { return "Print packingslip".ModLocalize("Print packingslip"); }
        }

        public static string PrintAddressLabel
        {
            get { return "Print address label".ModLocalize("Print address label"); }
        }

        public static string ErrMsgRequireAtleastOneItem
        {
            get { return "errmsg_Require_Atleast_One_Item".ModLocalize("No items selected"); }
        }

        public static string MsgNotificationIsSent
        {
            get { return "Msg notification is sent".ModLocalize("Notification is sent!"); }
        }
        public static string MsgErrorNotificationIsSent
        {
            get { return "Msg error notification is sent".ModLocalize("Notification is not sent!"); }
        }

        public static string MsgOfferEmailIsSent
        {
            get { return "Msg Offer Email is sent".ModLocalize("Offer email is sent!"); }
        }

        public static string MsgOfferEmailIsNotSent
        {
            get { return "Msg Offer Email is not sent".ModLocalize("Offer email is not sent!"); }
        }

        public static string SaveOffer
        {
            get { return "save offer".ModLocalize("Save & Send email"); }
        }

        public static string BodyNoteSendSMSNotification
        {
            get { return "Body note send SMS notification".ModLocalize("Sent SMS notification to customer on {0} by {1}. SMS info: {2}."); }
        }

        public static string BodyNoteSendEmailNotification
        {
            get { return "Body note send email notification".ModLocalize("Sent email notification to customer on {0} by {1}"); }
        }

        public static string BodyLogUpdatedDelivery
        {
            get { return "Body log updated delivery".ModLocalize("Updated delivery information. Former values : {0} Updated by: {1}"); }
        }
        public static string BodyLogUpdatedBilling
        {
            get { return "Body log updated billing ".ModLocalize("Updated billing  information. Former values : {0} Updated by: {1}"); }
        }
        public static string BodyLogRegisteredBackorder
        {
            get { return "Body log registered backorder".ModLocalize("Registered backorder with ID {0} by {1}"); }
        }
        public static string BodyLogRegisteredNewBackorderFrom
        {
            get { return "Body log registered new backorder from".ModLocalize("Registered new backorder from order {0} by  {1}"); }
        }

        public static string OriginalOrder
        {
            get { return "Original order".ModLocalize("Original order"); }
        }

        public static string CapturedPayment
        {
            get { return "Captured payment".ModLocalize("Captured payment"); }
        }

        public static string BodyLogOrderProcessedSend(bool shippingTracking = true)
        {
            if (shippingTracking)
                return "Body log order processed sens".ModLocalize("Order has been processed and SENT to customer at {0} by {1}. Tracking order number: {2}");
            else
                return "Body log order processed sens".ModLocalize("Order has been processed and SENT to customer at {0} by {1}.");
        }
        public static string BodyLogOrderCancelled
        {
            get { return "Body log order cancelled".ModLocalize("Order has been CANCELLED by {0}. Reason: {1}"); }
        }
        public static string CancelledTransactionPayment
        {
            get { return "Cancelled transaction payment".ModLocalize("Cancelled transaction payment"); }
        }
        public static string ErrMsgOrdercancelledFailed
        {
            get { return "ErrMsg_order_cancelled _failed".ModLocalize("Error during process transaction. Error message: {0}. Please cancelled this transaction manually"); }
        }

        public static string ErrMsgRestOrderOverlapOrigin
        {
            get { return "ErrMsg_restorder_overlap_origin".ModLocalize("The origin order has no product"); }
        }

        public static string YourPaymentHasBeenRefunded
        {
            get { return "Your payment has been refunded".ModLocalize("Your payment has been refunded. Transaction number: {0}"); }
        }

        public static string TrackingNumber
        {
            get { return "Tracking number".ModLocalize("Tracking number: {0}", false); }
        }

        public static string CustomerPersonal
        {
            get { return "Customer personal".ModLocalize("Personal"); }
        }

        public static string CustomerCompany
        {
            get { return "Customer company".ModLocalize("Company"); }
        }

        public static string OrderAddProduct
        {
            get { return "Order add product".ModLocalize("Add product", true); }
        }

        public static string TakeNoteOfflinePaymentAdmin
        {
            get { return "TakeNoteOfflinePaymentAdmin".ModLocalize("Order is created by admin at {0}", true); }
        }

        public static string CallbackLogPayment
        {
            get { return "Callback_Log_Payment".ModLocalize("The transaction number {0} was processed callback by {1} at {2}"); }
        }

        public static string CallbackPayment
        {
            get { return "Callback payment".ModLocalize("Callback payment"); }
        }

        public static string FreeshippingOrder
        {
            get { return "order Free shipping".ModLocalize("Free shipping", false); }
        }

        public static string FreeshippingOrderMembership
        {
            get { return "order Free shipping membership".ModLocalize("Free shipping (Membership)", false); }
        }

        public static string UpdateDelivered
        {
            get { return "Update delivered".ModLocalize(); }
        }

        public static string Customer
        {
            get { return "Customer".ModLocalize(); }
        }

        public static string PaymentAndShipping
        {
            get { return "Payment and Shipping".ModLocalize(); }
        }

        public static string DeliverAddress
        {
            get { return "Deliver address".ModLocalize("Deliver address", false); }
        }

        public static string Vat
        {
            get { return "Vat".ModLocalize("VAT", false); }
        }

        public static string ItemNotFound
        {
            get { return "Item not found".ModLocalize("Item not found", false); }
        }

        public static string Send
        {
            get { return "Send".ModLocalize("Send", false); }
        }

        public static string SendSMS
        {
            get { return "Send SMS".ModLocalize(); }
        }

        public static string DeliveryInformation
        {
            get { return "order delivery information".ModLocalize("Delivery information", false); }
        }

        public static string BillingInformation
        {
            get { return "order Billing information".ModLocalize("Billing information", false); }
        }

        public static string CompanyName
        {
            get { return "CompanyName".ModLocalize("Company"); }
        }

        public static string OrgNumber
        {
            get { return "OrgNumber".ModLocalize("Org#"); }
        }

        public static string ShippingBooking
        {
            get { return "Shipping booking".ModLocalize(); }
        }

        public static string ProteriaBooking
        {
            get { return "Proteria booking".ModLocalize(); }
        }

        public static string PostnordBooking
        {
            get { return "Postnord booking".ModLocalize(); }
        }

        public static string BookingServices
        {
            get { return "Booking services".ModLocalize("Services"); }
        }

        public static string SelectPickupPoint
        {
            get { return "Select pickup point".ModLocalize(); }
        }

        public static string SelectTermOfDelivery
        {
            get { return "Select term of delivery".ModLocalize(); }
        }

        public static string BringBooking
        {
            get { return "Bring booking".ModLocalize(); }
        }

        public static string BringMailBox
        {
            get { return "Bring mailbox".ModLocalize(); }
        }

        public static string BringBookingLogTempl
        {
            get { return "Bring Booking Log Templ".ModLocalize("Consignment#: {0}, Label url: {1}, Package#: {2}, Earliest pickup: {3}, Expected delivery: {4}"); }
        }

        public static string BringMailBoxLogTempl
        {
            get { return "Bring Mailbox Log Templ".ModLocalize("Id: {0}, Label url: {1}, Rfid label: {2}, Waybill: {3}"); }
        }

        public static string BringMailBoxSubmit
        {
            get { return "Bring mailbox Submit".ModLocalize("Submit shipping mailbox"); }
        }

        public static string BringMailBoxSaved
        {
            get { return "Bring mailbox Saved".ModLocalize("Shipping mailbox is ready for submit"); }
        }

        public static string BringMailBoxPending
        {
            get { return "Pending Bring mailbox".ModLocalize("Pending mailbox"); }
        }

        public static string DiscountPriceInc
        {
            get { return "Discount Price Inc".ModLocalize("Discount price (VAT)"); }
        }

        public static string ViewSaftData
        {
            get { return "View saft data".ModLocalize(); }
        }

        public static string SaftSignature
        {
            get { return "Saft Signature".ModLocalize("Signature"); }
        }

        public static string OrderImages
        {
            get { return "Order Images".ModLocalize("Images"); }
        }

        public static string DetailExpand
        {
            get { return "DetailExpand".ModLocalize("Expand"); }
        }

        public static string DetailCollapse
        {
            get { return "DetailCollapse".ModLocalize("Collapse"); }
        }

        public static string SelectPackageType
        {
            get { return "Select package type".ModLocalize(); }
        }

        public static string ProteriaBookingValidFailed
        {
            get { return "Proteria booking Valid Failed".ModLocalize("Invalid booking data! Please use web.proteria.com to correct the data"); }
        }

        public static string BookingPrint
        {
            get { return "Booking Print".ModLocalize("Print"); }
        }

        public static string ShippingBookingResultSubjectOk
        {
            get { return "Shipping Booking Result Subject Ok".ModLocalize("Booking shipping {0} succeeded"); }
        }

        public static string ShippingBookingResultSubjectFailed
        {
            get { return "Shipping Booking Result Subject Failed".ModLocalize("Booking shipping {0} failed"); }
        }

        public static string ShippingBookingResultBodyOk
        {
            get { return "Shipping Booking Result Body Ok".ModLocalize("BookingId: {0}, Tracking url: {1}"); }
        }

        public static string ShippingBookingResultBodyFailed
        {
            get { return "Shipping Booking Result Body Failed".ModLocalize("Error message: {0}"); }
        }

        public static string ShippingBookingValidationFailed
        {
            get { return "booking validation failed".ModLocalize("Validation failed"); }
        }

        public static string AddNewOpenProd
        {
            get { return "Add new open product".ModLocalize(); }
        }

        public static string OrderTotalMustNotGreaterThanReservedAmount
        {
            get { return "Order total must not be greater than the reserved amount".ModLocalize(); }
        }

        public static string OrderTilRegnskap
        {
            get { return "Order TilRegnskap".ModLocalize("TilRegnskap"); }
        }

        public static string OrderFlagCustom(int flag)
        {
            return ("Order flag custom " + flag).ModLocalize("Mark " + typeof(Domain.Interfaces.OrderFlagType).LocalizeEnumValue(flag)); 
        }

        public static string UpdateOrderFlagCustom(bool value)
        {
            return ("Update order flag custom " + value).ModLocalize(value ? "Marking" : "Unmaking");
        }

        public static string UpdateOrderFlagSent2PickupPoint
        {
            get { return ("Update order flag Sent2PickupPoint").ModLocalize("Order was sent to the pickup point"); }
        }

        public static string OrderFlagSent2PickupPoint
        {
            get { return ("order flag Sent2PickupPoint").ModLocalize("Send (pickup point)"); }
        }

        public static string OrderStatusTilRegnskap
        {
            get { return "Order Status TilRegnskap".ModLocalize("{0} (tilregnskap)"); }
        }

        public static string OrderStatusFlagCustom
        {
            get { return "Order Status flag custom".ModLocalize("{0} ({1})"); }
        }

        public static string OrderStatusRenewPayment
        {
            get { return "Order Status renew payment".ModLocalize("{0} (Venter på betaling)"); }
        }

        public static string OrderStatusSent2PickupPoint
        {
            get { return "Order Status Sent2PickupPoint".ModLocalize("{0} (Sent to pickup point)"); }
        }

        
        public static string OrderTilRegnskapSet
        {
            get { return "Order TilRegnskap was set".ModLocalize("TilRegnskap was set"); }
        }

        public static string AddEditBonger(int id)
        {
            if (id == 0) return "Add bonger".ModLocalize();
            else return "Edit bonger".ModLocalize();
        }

        public static string ManageBongers
        {
            get { return "Manage bongers".ModLocalize(); }
        }

        public static string Bonger
        {
            get { return "Bonger".ModLocalize(); }
        }

        public static string UpdatePickupPoint
        {
            get { return "Update pickup point".ModLocalize(); }
        }

        public static string Succeeded
        {
            get { return "Succeeded".ModLocalize(); }
        }

        public static string Failed
        {
            get { return "Failed".ModLocalize(); }
        }

        public static string PrintBookingLabel
        {
            get { return "Print booking label".ModLocalize(); }
        }

        public static string RecurOrderFailedSendNotiOk
        {
            get { return "RecurOrder Failed Send Noti ok".ModLocalize("An notification email was sent to customer successfully"); }
        }

        public static string RecurOrderFailedSendNotiOkRecurOneTime
        {
            get { return "RecurOrder Failed Send Noti ok RecurOneTime".ModLocalize("An notification email was sent to customer for retrying payment"); }
        }

        public static string RecurOrderFailedSendNotiFailed
        {
            get { return "RecurOrder Failed Send Noti failed".ModLocalize("Send email to customer failed with error: {0}"); }
        }

        public static string CancelAndDeactiveRecurOrder
        {
            get { return "Cancel and deactive recurring order".ModLocalize(); }
        }

        public static string CancelNoAttemptOrder
        {
            get { return "Cancel noAttempt order".ModLocalize("The order was canceled"); }
        }

        public static string SerialnumbersAndQuantityNotMatched
        {
            get { return "Serial number count and quantity is not matched".ModLocalize(); }
        }

        public static string ClickCollectSendByUser
        {
            get { return "Click Collect Send By User".ModLocalize("Sent by delivery manmager {0} at {1} "); }
        }

        public static string BundleGroupVariAccsSelectAll
        {
            get { return "Bundle Group Vari Accs Select All".ModLocalize("Please select a variant for each product"); }
        }

        public static string OfferProductIsNotAllowed
        {
            get { return "Offer Product Is Not Allowed".ModLocalize(); }
        }

        public static string OfferSmsMessageTempl
        {
            get { return "Offer Sms Message Templ".ModLocalize("Hei {{Order.DeliverName}}, \r\nda har vi sendt deg et tilbud på e-post adressen {{Order.Customer.Email}}. Om du ikke finner sjekk spam eller ta kontakt med oss på: jafi@jafi.no. Du kan og se tilbudet her: {{shop_url}}/cart?offerid={{Order.CartId}}&token={{Order.OfferToken}}"); }
        }

        public static string ShippingMaxParcelDimensionFormat
        {
            get { return "Shipping MaxParcelDimension format".ModLocalize("H x L x W: {0} x {1} x {2}"); }
        }
    }
}