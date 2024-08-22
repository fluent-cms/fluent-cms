type Profile = {
    id: string;
    email: string;
    roles: string[];
    fullAccessEntities: any[]; // Replace `any` with the specific type if known
    restrictedAccessEntities: any[]; // Replace `any` with the specific type if known
};
