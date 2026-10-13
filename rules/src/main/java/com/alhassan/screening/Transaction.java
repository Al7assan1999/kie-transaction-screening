package com.alhassan.screening;

/** A transaction submitted for screening. Plain fields + getters/setters so KIE Server can (un)marshal it as JSON. */
public class Transaction implements java.io.Serializable {

    private static final long serialVersionUID = 1L;

    private String id;
    private String customerId;
    private double amount;
    private String currency;
    /** ONLINE, BRANCH, ATM or CASH. */
    private String channel;
    /** ISO 3166-1 alpha-2 code of the counterparty country. */
    private String country;
    /** LOW, MEDIUM or HIGH, from the customer's KYC profile. */
    private String customerRiskLevel;
    /** Total of the customer's transactions today, including this one. */
    private double dailyTotal;
    private int accountAgeDays;

    public Transaction() {
    }

    public String getId() { return id; }
    public void setId(String id) { this.id = id; }

    public String getCustomerId() { return customerId; }
    public void setCustomerId(String customerId) { this.customerId = customerId; }

    public double getAmount() { return amount; }
    public void setAmount(double amount) { this.amount = amount; }

    public String getCurrency() { return currency; }
    public void setCurrency(String currency) { this.currency = currency; }

    public String getChannel() { return channel; }
    public void setChannel(String channel) { this.channel = channel; }

    public String getCountry() { return country; }
    public void setCountry(String country) { this.country = country; }

    public String getCustomerRiskLevel() { return customerRiskLevel; }
    public void setCustomerRiskLevel(String customerRiskLevel) { this.customerRiskLevel = customerRiskLevel; }

    public double getDailyTotal() { return dailyTotal; }
    public void setDailyTotal(double dailyTotal) { this.dailyTotal = dailyTotal; }

    public int getAccountAgeDays() { return accountAgeDays; }
    public void setAccountAgeDays(int accountAgeDays) { this.accountAgeDays = accountAgeDays; }
}
