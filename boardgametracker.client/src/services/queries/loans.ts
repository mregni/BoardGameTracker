import { QUERY_KEYS } from "@/models";
import { getLoansCall } from "../loanService";
import { createListQuery } from "./queryFactory";

export const getLoans = createListQuery(QUERY_KEYS.loans, getLoansCall);
