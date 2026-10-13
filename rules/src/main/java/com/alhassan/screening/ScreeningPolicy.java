package com.alhassan.screening;

import java.util.ArrayList;
import java.util.List;

/**
 * Configuration supplied with each request, so lists like high-risk countries can change
 * without redeploying the rules.
 */
public class ScreeningPolicy implements java.io.Serializable {

    private static final long serialVersionUID = 1L;

    /** ISO 3166-1 alpha-2 codes whose transactions need manual review. */
    private List<String> highRiskCountries = new ArrayList<>();

    public ScreeningPolicy() {
    }

    public ScreeningPolicy(List<String> highRiskCountries) {
        this.highRiskCountries = highRiskCountries;
    }

    public List<String> getHighRiskCountries() { return highRiskCountries; }
    public void setHighRiskCountries(List<String> highRiskCountries) { this.highRiskCountries = highRiskCountries; }
}
