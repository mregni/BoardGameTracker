import { CreateLoan } from '@/models/Loan/CreateLoan';
import { Loan } from '@/models/Loan/Loan';
import { axiosInstance } from '@/utils/axiosInstance';

const domain = 'loan';

export const getLoansCall = (): Promise<Loan[]> => {
  return axiosInstance.get<Loan[]>(domain).then((response) => {
    return response.data;
  });
};

export const saveLoanCall = (loan: CreateLoan): Promise<Loan> => {
  return axiosInstance.post<Loan>(domain, { ...loan }).then((response) => {
    return response.data;
  });
};

export const deleteLoanCall = (loanId: string): Promise<void> => {
  return axiosInstance.delete<void>(`${domain}/${loanId}`).then((response) => {
    return response.data;
  });
};

export const updateLoanCall = (loan: Loan): Promise<Loan> => {
  return axiosInstance.post<Loan>(domain, { ...loan }).then((response) => {
    return response.data;
  });
};
