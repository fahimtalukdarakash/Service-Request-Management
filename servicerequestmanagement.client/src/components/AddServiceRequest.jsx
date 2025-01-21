import React, { useState, useContext, useEffect } from "react";
import axios from "axios";
import { AuthContext } from "../AuthContext";

function AddServiceRequest() {
  const { isLoggedIn, userName, userRole, userID } = useContext(AuthContext);
  const [formData, setFormData] = useState({
    userID: "",
    taskDescription: "",
    requestType: "",
    project: "",
    startDate: "",
    endDate: "",
    totalManDays: "",
    numberOfSpecialists:"",
    numberOfOffers:"",
    location: "",
    providerManagerInfo: "",
    consumer: "",
    representatives: "",
    cycleStatus: "",
    masterAgreementID: "",
    masterAgreementName: "",
    selectedDomainName: "",
    roleSpecific: []
  });

  const [masterAgreements, setMasterAgreements] = useState([]);
  const [selectedAgreementId, setSelectedAgreementId] = useState("");
  const [domains, setDomains] = useState([]);
  const [roles, setRoles] = useState([]);
  const [levels, setLevels] = useState([]);
  const [technologyLevels, setTechnologyLevels] = useState([]);
  const [roleSpecificFields, setRoleSpecificFields] = useState([]);
  const [errors, setErrors] = useState({});
  const [message, setMessage] = useState("");

  useEffect(() => {
    if (userID) {
      setFormData((prev) => ({ ...prev, userID }));
    }
  }, [userID]);

  useEffect(() => {
    async function fetchMasterAgreements() {
      try {
        const response = await axios.get(
          "https://agiledev-contractandprovidermana-production.up.railway.app/master-agreements/established-agreements/"
        );
        setMasterAgreements(response.data);
      } catch (error) {
        console.error("Error fetching master agreements:", error);
      }
    }
    fetchMasterAgreements();
  }, []);

  const validate = () => {
    const errors = {};
    if (!formData.taskDescription) errors.taskDescription = "Task Description is required";
    if (!formData.requestType) errors.requestType = "Request Type is required";
    if (!formData.project) errors.project = "Project is required";
    if (!formData.startDate) errors.startDate = "Start Date is required";
    if (!formData.endDate) errors.endDate = "End Date is required";
    if (!formData.totalManDays) errors.totalManDays = "Total Man Days is required";
    if (!formData.numberOfSpecialists) errors.numberOfSpecialists = "Number of Specialists is required";
    if (!formData.numberOfOffers) errors.numberOfOffers = "Number of Offers is required";
    if (!formData.location) errors.location = "Location is required";
    if (!formData.consumer) errors.consumer = "Consumer is required";
    if (!formData.representatives) errors.representatives = "Representatives are required";
    if (!formData.masterAgreementID) errors.masterAgreementID = "Master Agreement is required";
    return Object.keys(errors).length === 0;
  };

  const handleAgreementChange = async (e) => {
    const agreementId = e.target.value;
    const selectedAgreement = masterAgreements.find(
      (agreement) => agreement.agreementId === parseInt(agreementId)
    );
  
    setFormData((prev) => ({
      ...prev,
      masterAgreementID: agreementId,
      masterAgreementName: selectedAgreement ? selectedAgreement.name : "",
      selectedDomainName: ""
    }));
    console.log(formData.masterAgreementName);
    setSelectedAgreementId(agreementId);
  
    // Fetch domains for the selected agreement
    if (agreementId) {
      try {
        const response = await axios.get(
          `https://agiledev-contractandprovidermana-production.up.railway.app/master-agreements/established-agreements/${agreementId}`
        );
        const domainsData = response.data.map((domain) => ({
          domainName: domain.domainName,
          roleDetails: domain.roleDetails
        }));
        setDomains(domainsData);
      } catch (error) {
        console.error("Error fetching domains:", error);
        setDomains([]); // Reset domains on error
      }
    } else {
      setDomains([]);
    }
  };
  
  const domainNameset = (domainName) =>{
    setFormData((prev) => ({ ...prev, selectedDomainName: domainName }));
  }
  const handleDomainChange = (e) => {
    const domainName = e.target.value;
    domainNameset(domainName);
    console.log(domainName);
    console.log(formData.masterAgreementID);
    console.log(formData.selectedDomainName);
    const domain = domains.find((d) => d.domainName === domainName);
    if (domain) {
      const uniqueRoles = [...new Set(domain.roleDetails.map((roleDetail) => roleDetail.role))];
      const uniqueLevels = [...new Set(domain.roleDetails.map((roleDetail) => roleDetail.level))];
      const uniqueTechnologyLevels = [...new Set(domain.roleDetails.map((roleDetail) => roleDetail.technologyLevel))];

      setRoles(uniqueRoles);
      setLevels(uniqueLevels);
      setTechnologyLevels(uniqueTechnologyLevels);
    } else {
      setRoles([]);
      setLevels([]);
      setTechnologyLevels([]);
    }
  };

  const handleRequestTypeChange = (type) => {
    setFormData((prev) => ({ ...prev, requestType: type }));
    if (type === "single") {
      setRoleSpecificFields([
        {
          role: "",
          level: "",
          technologyLevel: "",
          locationType: "",
          numberOfEmployee: 1,
          isDisabled: true
        }
      ]);
    } else if (type === "multi") {
      setRoleSpecificFields([
        {
          role: "",
          level: "",
          technologyLevel: "",
          locationType: "",
          numberOfEmployee: "",
          isDisabled: false
        }
      ]);
    } else {
      setRoleSpecificFields([]);
    }
  };

  const addRoleField = () => {
    setRoleSpecificFields((prevFields) => [
      ...prevFields,
      {
        role: "",
        level: "",
        technologyLevel: "",
        locationType: "",
        numberOfEmployee: "",
        isDisabled: false
      }
    ]);
  };

  const updateRoleField = (index, field, value) => {
    const updatedFields = [...roleSpecificFields];
    updatedFields[index][field] = value;
    setRoleSpecificFields(updatedFields);
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData({ ...formData, [name]: value });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) {
      setMessage("Please fill all required fields.");
      return;
    }

    const payload = {
      ...formData,
      roleSpecific: roleSpecificFields.map((field) => ({
        role: field.role,
        level: field.level,
        technologyLevel: field.technologyLevel,
        locationType: field.locationType,
        numberOfEmployee: field.numberOfEmployee
      }))
    };
    console.log(payload);
    try {
      const response = await axios.post(
        "https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/ServiceRequest/AddServiceRequest",
        payload
      );
      console.log("Service request submitted successfully:", response);
      setMessage("Service Request Submitted Successfully.");

      // Clear form data after successful submission
      setFormData({
        userID: userID || "",
        taskDescription: "",
        requestType: "",
        project: "",
        startDate: "",
        endDate: "",
        totalManDays: "",
        numberOfSpecialists:"",
        numberOfOffers:"",
        location: "",
        providerManagerInfo: "",
        consumer: "",
        representatives: "",
        cycleStatus: "",
        masterAgreementID: "",
        masterAgreementName: "",
        selectedDomainName: "",
        roleSpecific: []
      });
      setRoleSpecificFields([]);
    } catch (error) {
      console.error("Error submitting service request:", error);
      setMessage("An error occurred while submitting the service request.");
    }
  };

  return (
    <div className="card bg-secondary text-light p-4">
      <h2 className="card-title">Add a Service Request</h2>
      <form onSubmit={handleSubmit}>
      <div className="mb-3">
          <label htmlFor="project" className="form-label">Project</label>
          <input
            type="text"
            id="project"
            name="project"
            className={`form-control ${errors.project ? "is-invalid" : ""}`}
            value={formData.project}
            onChange={handleChange}
          />
          {errors.project && <div className="invalid-feedback">{errors.project}</div>}
        </div>
        <div className="mb-3">
          <label htmlFor="taskDescription" className="form-label">
            Task Description
          </label>
          <textarea
            id="taskDescription"
            name="taskDescription"
            className={`form-control ${errors.taskDescription ? "is-invalid" : ""}`}
            value={formData.taskDescription}
            onChange={handleChange}
          ></textarea>
          {errors.taskDescription && <div className="invalid-feedback">{errors.taskDescription}</div>}
        </div>
        <div className="mb-3">
          <label htmlFor="startDate" className="form-label">Start Date</label>
          <input
            type="date"
            id="startDate"
            name="startDate"
            className={`form-control ${errors.startDate ? "is-invalid" : ""}`}
            value={formData.startDate}
            onChange={handleChange}
          />
          {errors.startDate && <div className="invalid-feedback">{errors.startDate}</div>}
        </div>
        <div className="mb-3">
          <label htmlFor="endDate" className="form-label">End Date</label>
          <input
            type="date"
            id="endDate"
            name="endDate"
            className={`form-control ${errors.endDate ? "is-invalid" : ""}`}
            value={formData.endDate}
            onChange={handleChange}
          />
          {errors.endDate && <div className="invalid-feedback">{errors.endDate}</div>}
        </div>
        {/* Total Man Days */}
        <div className="mb-3">
          <label htmlFor="totalManDays" className="form-label">Total Man Days</label>
          <input
            type="number"
            id="totalManDays"
            name="totalManDays"
            className={`form-control ${errors.totalManDays ? "is-invalid" : ""}`}
            value={formData.totalManDays}
            onChange={handleChange}
          />
          {errors.totalManDays && <div className="invalid-feedback">{errors.totalManDays}</div>}
        </div>
        {/* Number of Specialists Required */}
        <div className="mb-3">
          <label htmlFor="numberOfSpecialists" className="form-label">Number of Specialists</label>
          <input
            type="number"
            id="numberOfSpecialists"
            name="numberOfSpecialists"
            className={`form-control ${errors.numberOfSpecialists ? "is-invalid" : ""}`}
            value={formData.numberOfSpecialists}
            onChange={handleChange}
          />
          {errors.numberOfSpecialists && <div className="invalid-feedback">{errors.numberOfSpecialists}</div>}
        </div>
        {/* Number of Offers */}
        <div className="mb-3">
          <label htmlFor="numberOfOffers" className="form-label">Number of Offers</label>
          <input
            type="number"
            id="numberOfOffers"
            name="numberOfOffers"
            className={`form-control ${errors.numberOfOffers ? "is-invalid" : ""}`}
            value={formData.numberOfOffers}
            onChange={handleChange}
          />
          {errors.numberOfOffers && <div className="invalid-feedback">{errors.numberOfOffers}</div>}
        </div>
        {/* Location */}
        <div className="mb-3">
          <label htmlFor="location" className="form-label">Location</label>
          <input
            type="text"
            id="location"
            name="location"
            className={`form-control ${errors.location ? "is-invalid" : ""}`}
            value={formData.location}
            onChange={handleChange}
          />
          {errors.location && <div className="invalid-feedback">{errors.location}</div>}
        </div>

        {/* Provider Manager Info */}
        <div className="mb-3">
          <label htmlFor="providerManagerInfo" className="form-label">Provider Manager Info</label>
          <textarea
            id="providerManagerInfo"
            name="providerManagerInfo"
            className="form-control"
            value={formData.providerManagerInfo}
            onChange={handleChange}
          ></textarea>
        </div>

        {/* Consumer */}
        <div className="mb-3">
          <label htmlFor="consumer" className="form-label">Consumer</label>
          <input
            type="text"
            id="consumer"
            name="consumer"
            className={`form-control ${errors.consumer ? "is-invalid" : ""}`}
            value={formData.consumer}
            onChange={handleChange}
          />
          {errors.consumer && <div className="invalid-feedback">{errors.consumer}</div>}
        </div>

        {/* Representatives */}
        <div className="mb-3">
          <label htmlFor="representatives" className="form-label">Representatives</label>
          <textarea
            id="representatives"
            name="representatives"
            className={`form-control ${errors.representatives ? "is-invalid" : ""}`}
            value={formData.representatives}
            onChange={handleChange}
          ></textarea>
          {errors.representatives && <div className="invalid-feedback">{errors.representatives}</div>}
        </div>
        <div className="mb-3">
          <label htmlFor="masterAgreement" className="form-label">Master Agreement</label>
          <select
            id="masterAgreement"
            className="form-control"
            onChange={handleAgreementChange}
          >
            <option value="">Select Master Agreement</option>
            {masterAgreements.map((agreement) => (
              <option key={agreement.agreementId} value={agreement.agreementId}>
                {agreement.name}
              </option>
            ))}
          </select>
        </div>

        <div className="mb-3">
          <label htmlFor="domain" className="form-label">Domain</label>
          <select
            id="domain"
            className="form-control"
            //value={formData.domainName}
            //onChange={(e) => handleDomainChange(e.target.value)}
            onChange={handleDomainChange}
          >
            <option value="">Select Domain</option>
            {domains.map((domain, index) => (
              <option key={index} value={domain.domainName}>
                {domain.domainName}
              </option>
            ))}
          </select>
        </div>

        <div className="mb-3">
          <label htmlFor="requestType" className="form-label">Request Type</label>
          <select
            id="requestType"
            className="form-control"
            onChange={(e) => handleRequestTypeChange(e.target.value)}
          >
            <option value="">Select Request Type</option>
            <option value="single">Single</option>
            <option value="multi">Multi</option>
            <option value="team">Team</option>
          </select>
        </div>
        <div className="mb-3">
          <label htmlFor="cycleStatus" className="form-label">Cycle</label>
          <select
            id="cycleStatus"
            className="form-control"
            value={formData.cycleStatus}
            onChange={(e) => setFormData({ ...formData, cycleStatus: e.target.value })}
          >
            <option value="">Select Cycle</option>
            <option value="1">Cycle 1</option>
            <option value="2">Cycle 2</option>
          </select>
        </div>
        {roleSpecificFields.map((field, index) => (
          <div key={index} className="mb-3">
            <div className="row">
              <div className="col-md-3">
                <label htmlFor={`role-${index}`} className="form-label">Role</label>
                <select
                  id={`role-${index}`}
                  className="form-control"
                  value={field.role}
                  onChange={(e) => updateRoleField(index, "role", e.target.value)}
                >
                  <option value="">Select Role</option>
                  {roles.map((role, i) => (
                    <option key={i} value={role}>
                      {role}
                    </option>
                  ))}
                </select>
              </div>

              <div className="col-md-2">
                <label htmlFor={`level-${index}`} className="form-label">Level</label>
                <select
                  id={`level-${index}`}
                  className="form-control"
                  value={field.level}
                  onChange={(e) => updateRoleField(index, "level", e.target.value)}
                >
                  <option value="">Select Level</option>
                  {levels.map((level, i) => (
                    <option key={i} value={level}>
                      {level}
                    </option>
                  ))}
                </select>
              </div>

              <div className="col-md-3">
                <label htmlFor={`technologyLevel-${index}`} className="form-label">Technology Level</label>
                <select
                  id={`technologyLevel-${index}`}
                  className="form-control"
                  value={field.technologyLevel}
                  onChange={(e) => updateRoleField(index, "technologyLevel", e.target.value)}
                >
                  <option value="">Select Technology Level</option>
                  {technologyLevels.map((techLevel, i) => (
                    <option key={i} value={techLevel}>
                      {techLevel}
                    </option>
                  ))}
                </select>
              </div>

              <div className="col-md-2">
                <label htmlFor={`locationType-${index}`} className="form-label">Location Type</label>
                <select
                  id={`locationType-${index}`}
                  className="form-control"
                  value={field.locationType}
                  onChange={(e) => updateRoleField(index, "locationType", e.target.value)}
                >
                  <option value="">Select Location Type</option>
                  <option value="onsite">Onsite</option>
                  <option value="hybrid">Hybrid</option>
                  <option value="remote">Remote</option>
                </select>
              </div>

              <div className="col-md-2">
                <label htmlFor={`numberOfEmployee-${index}`} className="form-label">Number of Employee</label>
                <input
                  type="number"
                  id={`numberOfEmployee-${index}`}
                  className="form-control"
                  value={field.numberOfEmployee}
                  onChange={(e) => updateRoleField(index, "numberOfEmployee", e.target.value)}
                  disabled={field.isDisabled}
                />
              </div>
            </div>
          </div>
        ))}

        {formData.requestType === "team" && (
          <button type="button" className="btn btn-secondary" onClick={addRoleField}>
            Add Role
          </button>
        )}

        <button type="submit" className="btn btn-primary mt-3">
          Submit
        </button>
      </form>
    </div>
  );
}

export default AddServiceRequest;
