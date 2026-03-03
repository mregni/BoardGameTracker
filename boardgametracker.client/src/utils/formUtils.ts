interface FormLike {
	handleSubmit: () => void;
}

export const handleFormSubmit = (form: FormLike) => (e: React.FormEvent) => {
	e.preventDefault();
	e.stopPropagation();
	form.handleSubmit();
};
