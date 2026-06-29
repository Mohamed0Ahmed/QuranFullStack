SELECT candidate_status, COUNT(*) AS n
FROM classified GROUP BY candidate_status ORDER BY n DESC;
