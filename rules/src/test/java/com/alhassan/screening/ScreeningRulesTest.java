package com.alhassan.screening;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertTrue;

import java.util.Arrays;
import java.util.List;

import org.junit.Test;
import org.kie.api.KieServices;
import org.kie.api.command.KieCommands;
import org.kie.api.runtime.StatelessKieSession;

public class ScreeningRulesTest {

    private static final List<String> HIGH_RISK = Arrays.asList("AA", "BB");

    private ScreeningResult screen(Transaction t) {
        StatelessKieSession session = KieServices.Factory.get()
                .getKieClasspathContainer()
                .newStatelessKieSession("screeningSession");

        ScreeningResult result = new ScreeningResult(t.getId());
        KieCommands commands = KieServices.Factory.get().getCommands();
        session.execute(commands.newBatchExecution(Arrays.asList(
                commands.newInsert(t),
                commands.newInsert(new ScreeningPolicy(HIGH_RISK)),
                commands.newInsert(result),
                commands.newFireAllRules())));
        return result;
    }

    private static Transaction tx(double amount) {
        Transaction t = new Transaction();
        t.setId("T1");
        t.setCustomerId("C1");
        t.setAmount(amount);
        t.setCurrency("USD");
        t.setChannel("ONLINE");
        t.setCountry("US");
        t.setCustomerRiskLevel("LOW");
        t.setDailyTotal(amount);
        t.setAccountAgeDays(365);
        return t;
    }

    @Test
    public void ordinaryTransactionIsApproved() {
        ScreeningResult r = screen(tx(250));

        assertEquals(ScreeningResult.APPROVE, r.getDecision());
        assertEquals(0, r.getRiskScore());
        assertTrue(r.getReasons().isEmpty());
    }

    @Test
    public void exceedingDailyLimitIsRejected() {
        Transaction t = tx(5000);
        t.setDailyTotal(52000);

        ScreeningResult r = screen(t);

        assertEquals(ScreeningResult.REJECT, r.getDecision());
    }

    @Test
    public void highRiskCountryNeedsReview() {
        Transaction t = tx(100);
        t.setCountry("AA");

        assertEquals(ScreeningResult.REVIEW, screen(t).getDecision());
    }

    @Test
    public void rejectWinsOverReviewAndScoresAccumulate() {
        Transaction t = tx(12000);
        t.setChannel("CASH");          // review, 15
        t.setCustomerRiskLevel("HIGH"); // review, 25
        t.setDailyTotal(60000);         // reject, 50

        ScreeningResult r = screen(t);

        assertEquals(ScreeningResult.REJECT, r.getDecision());
        assertEquals(90, r.getRiskScore());
        assertEquals(3, r.getReasons().size());
    }

    @Test
    public void newAccountWithLargeAmountNeedsReview() {
        Transaction t = tx(6000);
        t.setAccountAgeDays(10);

        assertEquals(ScreeningResult.REVIEW, screen(t).getDecision());
    }
}
