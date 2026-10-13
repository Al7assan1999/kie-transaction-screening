package com.alhassan.screening;

import java.util.ArrayList;
import java.util.List;

/**
 * The outcome of screening one transaction. Rules only ever escalate the decision
 * (APPROVE -> REVIEW -> REJECT), so rule firing order never weakens a result.
 */
public class ScreeningResult implements java.io.Serializable {

    private static final long serialVersionUID = 1L;

    public static final String APPROVE = "APPROVE";
    public static final String REVIEW = "REVIEW";
    public static final String REJECT = "REJECT";

    private String transactionId;
    private String decision = APPROVE;
    private int riskScore;
    private List<String> reasons = new ArrayList<>();

    public ScreeningResult() {
    }

    public ScreeningResult(String transactionId) {
        this.transactionId = transactionId;
    }

    /** Called from rules: record why, add to the score, and escalate the decision if needed. */
    public void flag(String decision, int score, String reason) {
        if (rank(decision) > rank(this.decision)) {
            this.decision = decision;
        }
        this.riskScore += score;
        this.reasons.add(reason);
    }

    private static int rank(String decision) {
        switch (decision) {
            case REJECT: return 2;
            case REVIEW: return 1;
            default: return 0;
        }
    }

    public String getTransactionId() { return transactionId; }
    public void setTransactionId(String transactionId) { this.transactionId = transactionId; }

    public String getDecision() { return decision; }
    public void setDecision(String decision) { this.decision = decision; }

    public int getRiskScore() { return riskScore; }
    public void setRiskScore(int riskScore) { this.riskScore = riskScore; }

    public List<String> getReasons() { return reasons; }
    public void setReasons(List<String> reasons) { this.reasons = reasons; }
}
