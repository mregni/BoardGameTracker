import { axiosInstance } from '@/utils/axiosInstance';
import { Loan } from '@/models/Loan/Loan';
import { CreateLoan } from '@/models/Loan/CreateLoan';

const domain = 'loans';

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

export const deleteLoanCall = (id: number): Promise<void> => {
  return axiosInstance.delete<void>(`${domain}/${id}`).then((response) => {
    return response.data;
  });
};

export const updateLoanCall = (loan: Loan): Promise<Loan> => {
  return axiosInstance.put<Loan>(domain, { ...loan }).then((response) => {
    return response.data;
  });
};

export const returnLoanCall = (id: number, returnDate: Date): Promise<Loan> => {
  return axiosInstance.put<Loan>(`${domain}/return`, { id, returnDate }).then((response) => {
    return response.data;
  });
};
